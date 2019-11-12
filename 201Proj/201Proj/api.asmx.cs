using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Web.Script.Serialization;
using System.Web.Script.Services;
using System.Web.Services;
using System.Xml;
using System.Xml.Serialization;

namespace StahrDBAPI {
	/// <summary>
	/// 
	///		Author:			Mike Stahr
	///		Created:		9-20-2017
	///		Last Updated:	6-8-2019
	///
	///		Last Update:	Updated encryption
	/// </summary>
	[WebService(Namespace = "http://tempuri.org/")]
	[WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
	[System.ComponentModel.ToolboxItem(false)]
	[System.Web.Script.Services.ScriptService]
	public class api : System.Web.Services.WebService {
		
		public static string SHARED_SECRET = ConfigurationManager.AppSettings["sharedSecret"];
		private static int EncryptionPadding = 2048;    		// Note: Small=1024, Big=2048
		private string encryptionKey = ConfigurationManager.AppSettings["encryptionKeys_" + EncryptionPadding];

		private const string emailPasscode = "Put some random string of characters here that will need to be sent to the ";
		private const string cryptoSalt = "Put some random string here used as 'salt' when encrypting";

		#region ######################################################################################################################################################## Database Stuff
		private const string dbConfig = "DefaultConnection";

		private string conn = System.Configuration.ConfigurationManager.ConnectionStrings[dbConfig].ConnectionString;
		private List<SqlParameter> parameters = new List<SqlParameter>();

		// This method is used in conjunction with a "user defined table" in the database
		public DataTable sqlExec(string sql, DataTable dt, string udtblParam) {
			DataTable ret = new DataTable();

			try {
				using (SqlConnection objConn = new SqlConnection(conn)) {
					SqlCommand cmd = new SqlCommand(sql, objConn);
					cmd.CommandType = CommandType.StoredProcedure;
					SqlParameter tvparam = cmd.Parameters.AddWithValue(udtblParam, dt);
					tvparam.SqlDbType = SqlDbType.Structured;
					objConn.Open();
					ret.Load(cmd.ExecuteReader(CommandBehavior.CloseConnection));
				}
			} catch (Exception e) {
				setDataTableToError(ret, e);
			}
			parameters.Clear();
			return ret;
		}

		public DataTable sqlExec(string sql) {
			return sqlExecDataTable(sql);
		}

		public Object sqlExecFunction(string fn) {
			DataSet userDataset = new DataSet();
			Object ret = null;
			try {
				using (SqlConnection objConn = new SqlConnection(conn)) {
					objConn.Open();
					SqlCommand command = new SqlCommand(fn, objConn);
					command.CommandType = CommandType.Text;
					command.Parameters.AddRange(parameters.ToArray());
					ret = command.ExecuteScalar();
					objConn.Close();
				}
			} catch (Exception e) {
				throw e;
			}

			parameters.Clear();
			return ret;
		}

		public DataTable sqlExecDataTable(string sql) {
			DataSet userDataset = new DataSet();

			using (SqlConnection objConn = new SqlConnection(conn)) {
				try {
					SqlDataAdapter myCommand = new SqlDataAdapter(sql, objConn);
					myCommand.SelectCommand.CommandTimeout = 5;
					myCommand.SelectCommand.CommandType = CommandType.StoredProcedure;
					myCommand.SelectCommand.Parameters.AddRange(parameters.ToArray());
					myCommand.Fill(userDataset);

					parameters.Clear();
					if (userDataset.Tables.Count == 0) userDataset.Tables.Add();
					return userDataset.Tables[0];

				} catch (Exception e) {
					throw e;
				}
			}
		}

		public DataSet sqlExecDataSet(string sql) {

			DataSet userDataset = new DataSet();
			try {
				using (SqlConnection objConn = new SqlConnection(conn)) {
					SqlDataAdapter myCommand = new SqlDataAdapter(sql, objConn);
					myCommand.SelectCommand.CommandType = CommandType.StoredProcedure;
					myCommand.SelectCommand.Parameters.AddRange(parameters.ToArray());
					myCommand.Fill(userDataset);
				}
			} catch (Exception e) {
				userDataset.Tables.Add();
				setDataTableToError(userDataset.Tables[0], e);
			}

			parameters.Clear();
			return userDataset;
		}

		private void setDataTableToError(DataTable tbl, Exception e) {

			tbl.Columns.Add(new DataColumn("Error", typeof(Exception)));

			DataRow row = tbl.NewRow();
			row["Error"] = e;
			try {
				tbl.Rows.Add(row);
			} catch (Exception) { }
		}

		public void addParam(string name, object value) {
			parameters.Add(new SqlParameter(name, value));
		}

		#endregion

		#region ######################################################################################################################################################## Serializer
		private enum serializeStyle {
			GENERAL,
			DATA_SET,
			DATA_TABLE,
			DICTIONARY,
			STREAM_JSON,
			OBJECT,
			SINGLE_TABLE_ROW,
			JSON_RETURN
		}

		private void send(object obj) {
			send(obj, serializeStyle.DATA_TABLE);
		}

		private void send(object obj, serializeStyle style) {
			try {
				switch (style) {
					case serializeStyle.DATA_SET: serializeDataSet(sqlExecDataSet((string)obj)); break;
					case serializeStyle.DATA_TABLE: serializeDataTable(sqlExecDataTable((string)obj)); break;
					case serializeStyle.OBJECT: serializeObject(obj); break;
					case serializeStyle.SINGLE_TABLE_ROW: serializeSingleDataTableRow(sqlExecDataTable((string)obj)); break;
					case serializeStyle.DICTIONARY: serializeDictionary((Dictionary<object, object>)obj); break;
					case serializeStyle.STREAM_JSON: streamJson((string)obj); break;
					case serializeStyle.GENERAL: serialize(obj); break;
					case serializeStyle.JSON_RETURN: streamJson(sqlExecDataSet((string)obj)); break;
					default: serialize("Invalid serialization"); break;
				}
			} catch (Exception e) {
				serialize("Error during send(): " + e.Message);
			}
		}

		private List<Dictionary<string, object>> getTableRows(DataTable dt) {
			List<Dictionary<string, object>> rows = new List<Dictionary<string, object>>();
			Dictionary<string, object> row;
			row = new Dictionary<string, object>();
			foreach (DataRow dr in dt.Rows) {
				row = new Dictionary<string, object>();
				foreach (DataColumn col in dt.Columns)
					row.Add(col.ColumnName, dr[col]);
				rows.Add(row);
			}
			return rows;
		}

		// Streams out a JSON string
		public void streamJson(string jsonString) {
			try {
				HttpContext.Current.Response.Clear();
				HttpContext.Current.Response.ContentType = "application/json";
				HttpContext.Current.Response.StatusCode = 200;
				HttpContext.Current.Response.StatusDescription = "";
				HttpContext.Current.Response.AddHeader("content-length", Encoding.UTF8.GetBytes(jsonString).Length.ToString());
				// or can ...
				//HttpContext.Current.Response.AddHeader("content-length", ""+ Buffer.ByteLength(Encoding.UTF8.GetBytes(jsonString)));
				HttpContext.Current.Response.Write(jsonString);
				HttpContext.Current.Response.Flush();
				HttpContext.Current.ApplicationInstance.CompleteRequest();
			} catch { }
		}

		// NEW - Converts a returned JSON dataset to a json object. Use: FOR JSON PATH in SQL Server
		public void streamJson(DataSet ds) {
			string ret = "";
			try {
				foreach (DataTable dt in ds.Tables)
					foreach (DataRow dr in dt.Rows)
						ret += dr.ItemArray[0];
			} catch (Exception e) {
				ret = "";
			}

			streamJson(ret);
		}

		// A method to create a json string from an object
		public string buildJson(Object obj) {
			try {
				return (new JavaScriptSerializer().Serialize(obj));
			} catch (Exception e) {
				return (new JavaScriptSerializer().Serialize("Invalid serializable object. " + e.Source));
			}
		}

		// Simple method to serialize an object into a JSON string and write it to the Response Stream
		public void serialize(Object obj) {
			streamJson(buildJson(obj));

			//try {
			//	streamJson(new JavaScriptSerializer().Serialize(obj));
			//} catch (Exception e) {
			//	streamJson(new JavaScriptSerializer().Serialize("Invalid serializable object. " + e.Source));
			//}
		}

		// Generate and serialize a single row from a returned data table. Method will only return the first row - even if there are more.
		public void serializeSingleDataTableRow(DataTable dt) {
			serializeSingleDataTableRow(dt, "");
		}

		public void serializeSingleDataTableRow(DataTable dt, params string[] excludeColumns) {
			Dictionary<string, object> row = new Dictionary<string, object>();

			if (dt.Rows.Count > 0)
				foreach (DataColumn col in dt.Columns)
					if (!excludeColumns.Contains(col.ColumnName))
						row.Add(col.ColumnName, dt.Rows[0][col]);
			serialize(row);
		}

		// Serialize an entire table retrieved from a data call
		public void serializeDataTable(DataTable dt) {
			serialize(getTableRows(dt));
		}

		// Serialize an multiple tables retrieved from a data call
		public void serializeDataSet(DataSet ds) {
			List<object> ret = new List<object>();

			foreach (DataTable dt in ds.Tables)
				ret.Add(getTableRows(dt));
			serialize(ret);
		}

		// Converting an object to XML status
		public void serializeXML<T>(T value) {
			string ret = "";

			if (value != null) {
				try {
					HttpContext.Current.Response.Clear();
					HttpContext.Current.Response.ContentType = "text/xml";

					var xmlserializer = new XmlSerializer(typeof(T));
					var stringWriter = new StringWriter();

					using (var writer = XmlWriter.Create(stringWriter)) {
						xmlserializer.Serialize(writer, value);
						ret = stringWriter.ToString();
					}
				} catch (Exception) { }
				HttpContext.Current.Response.Write(ret);
				HttpContext.Current.Response.Flush();
				HttpContext.Current.ApplicationInstance.CompleteRequest();
			}
		}

		// Serialize a dictionary object to avoid having to create more classes
		public void serializeDictionary(Dictionary<object, object> dic) {
			serialize(dic.ToDictionary(item => item.Key.ToString(), item => item.Value.ToString()));
		}
		// NOTE: In order to use the following methods you will need to install Newtonsoft.Json into your project. Look this up on Google
		// Using generics this method will serialize a JSON package into a class structure or return a new instance of the class on error
		public T _download_serialized_json_data<T>(string url) where T : new() {
			using (var w = new WebClient()) {
				try { return JsonConvert.DeserializeObject<T>(w.DownloadString(url)); } catch (Exception) { return new T(); }
			}
		}

		public T deserialize<T>(string obj) where T : new() {
			//======================================================================= Example
			//		See below for example
			//=======================================================================
			try { return JsonConvert.DeserializeObject<T>(obj); } catch (Exception) { return new T(); }
		}

		/*//======================================================================================= EXAMPLE
			//[WebMethod]
			//public void sampleLogin() {
			//	CurrentUser u = new CurrentUser() { accountGUID = "C82C926F-E984-4710-B142-D2AAFB8FF9A3", startOfWeek = 1, units = 1, userName = "Hoya" };
			//	writeCookie(Globals._r, convertObjToJSON(u), 8760); // 8760 = hours in a year
			//}
			//
			//[WebMethod]
			//public void sampleGetUser() {
			//	CurrentUser cu = deserialize<CurrentUser>(readCookie(Globals._r));
			//	serialize(deserialize<CurrentUser>(readCookie(Globals._r)));
			//}

			//private class CurrentUser {
			//	public string accountGUID { get; set; }
			//	public string userName { get; set; }
			//	public int startOfWeek { get; set; } // 0 = sunday, 1 = monday
			//	public int units { get; set; } // 1 = Miles, 2 = Meters, 3 = Kilometers, 9 = Yards, 10 = Meter (hurdles), 12 = Minutes, 13 = Seconds
			//}
		*/

		// Probably don't need this as one can just type "serialize(object to serialize);" but if every we do we have it.   
		// Not sure it will work for objects that have arrays of other objects though...
		public void serializeObject(Object obj) {
			Dictionary<string, object> row = new Dictionary<string, object>();
			row = new Dictionary<string, object>();
			var prop = obj.GetType().GetProperties();

			foreach (var props in prop)
				row.Add(props.Name, props.GetGetMethod().Invoke(obj, null));
			serialize(row);
		}

		public string convertObjToJSON(object o) {
			JavaScriptSerializer js = new JavaScriptSerializer();
			return js.Serialize(o);
		}

		public dynamic getJsonObject(string json) {
			return Newtonsoft.Json.JsonConvert.DeserializeObject(json);
			//================================================================= Use Example
			//dynamic j = getJsonObject(json);
			//string output = String.Format("{0}, {1} {2}", j.accountGUID, j.firstName, j.lastName); 			
		}

		#endregion

		#region ######################################################################################################################################################## Internal Methods

		// Uncomment the following 2 lines and run API to generate your encryption codes. 
		// Copy and past output to your Web.config file 

		//[WebMethod] public void generate_1024_Key() {streamJson(getEncryptedString(1024));}
		//[WebMethod] public void generate_2048_Key() {streamJson(getEncryptedString(2048));}
		
		private string getEncryptedString(int EncryptionPadding) {
			var csp = new RSACryptoServiceProvider(EncryptionPadding);
			var key = csp.ExportParameters(true);
			string KeyString;
			{
				var sw = new System.IO.StringWriter();
				var xs = new System.Xml.Serialization.XmlSerializer(typeof(RSAParameters));
				xs.Serialize(sw, key);
				KeyString = sw.ToString();
			}
			return (string.Format("&lt;RSAKeyValue&gt;&lt;Exponent&gt;{0}&lt;/Exponent&gt;&lt;Modulus&gt;{1}&lt;/Modulus&gt;&lt;P&gt;{2}&lt;/P&gt;&lt;Q&gt;{3}&lt;/Q&gt;&lt;DP&gt;{4}&lt;/DP&gt;&lt;DQ&gt;{5}&lt;/DQ&gt;&lt;InverseQ&gt;{6}&lt;/InverseQ&gt;&lt;D&gt;{7}&lt;/D&gt;&lt;/RSAKeyValue&gt;",
										Convert.ToBase64String(key.Exponent),
										Convert.ToBase64String(key.Modulus),
										Convert.ToBase64String(key.P),
										Convert.ToBase64String(key.Q),
										Convert.ToBase64String(key.DP),
										Convert.ToBase64String(key.DQ),
										Convert.ToBase64String(key.InverseQ),
										Convert.ToBase64String(key.D)
									)
					  );
		}

		public string encrypt(string data) {
			try {
				return api.Crypto.EncryptStringAES(data, SHARED_SECRET);
			} catch {
				return data;
			}

			////SHA512 sha512 = SHA512Managed.Create();
			////byte[] bytes = Encoding.UTF8.GetBytes(data);
			////byte[] hash = sha512.ComputeHash(bytes);

			////StringBuilder result = new StringBuilder();
			////for (int i = 0; i < hash.Length; i++) {
			////	result.Append(hash[i].ToString("X2"));
			////}
			////return result.ToString();

			//using (var rsa = new RSACryptoServiceProvider(EncryptionPadding)) {
			//	try {
			//		rsa.FromXmlString(encryptionKey);
			//		var encryptedData = rsa.Encrypt(Encoding.UTF8.GetBytes(data), true);
			//		var base64Encrypted = Convert.ToBase64String(encryptedData);
			//		return base64Encrypted;
			//	} catch (Exception e) {
			//		return e.Message;
			//	} finally {
			//		rsa.PersistKeyInCsp = false;
			//	}
			//}
		}

		public string dencrypt(string data) {
			try {
				return Crypto.DecryptStringAES(data, SHARED_SECRET);
			} catch {
				return data;
			}

			//try {
			//	using (var rsa = new RSACryptoServiceProvider(EncryptionPadding)) {
			//		try {
			//			var base64Encrypted = data;
			//			rsa.FromXmlString(encryptionKey);
			//			var resultBytes = Convert.FromBase64String(base64Encrypted);
			//			var decryptedBytes = rsa.Decrypt(resultBytes, true);
			//			var decryptedData = Encoding.UTF8.GetString(decryptedBytes);
			//			return decryptedData.ToString();
			//		} finally {
			//			rsa.PersistKeyInCsp = false;
			//		}
			//	}
			//} catch {}
			//return "";
		}

		// Writes an encrypted cookie. If expiresHours=0 then no expiration
		public void writeCookie(string name, string value, double expiresHours, bool encryptValue = true) {
			HttpCookie cookie = new HttpCookie(name);
			cookie.Value = (encryptValue ? encrypt(value) : value);
			if (expiresHours != 0) cookie.Expires = DateTime.Now.AddHours(expiresHours);
			HttpContext.Current.Response.Cookies.Add(cookie);
		}

		// Reads an encrypted cookie and decrypts it
		public string readCookie(string name, bool dencryptValue = true) {
			HttpRequest Request = System.Web.HttpContext.Current.Request;
			if (Request.Cookies[name] != null) return (dencryptValue ? dencrypt(Request.Cookies[name].Value) : Request.Cookies[name].Value);
			return "";
		}

		public void deleteCookie(string name) {
			writeCookie(name, "", -24, false);
		}

		// Sends an email out using the user's credentials from the web.config file.
		public void sendEmail(string from, string to, string cc, string bcc, string subject, string message) {
			SmtpClient mailClient = null;
			try {
				string pw = ConfigurationManager.AppSettings["emailPassWord"];
				string fromAddress = ConfigurationManager.AppSettings["emailFromAddress"];
				mailClient = new SmtpClient("smtp.gmail.com", 587);  //'465
				NetworkCredential cred = new NetworkCredential(fromAddress, pw);
				MailMessage msg = new MailMessage();
				msg.IsBodyHtml = true;
				msg.From = new MailAddress(from);
				msg.To.Add(to);
				msg.Subject = subject;
				msg.Body = "<!DOCTYPE html><html><head><title>Email</title></head><body>" + HttpUtility.HtmlDecode(message) + "</body></html>";
				msg.ReplyToList.Add(from);
				if (cc.Trim().Length > 0) msg.CC.Add(cc);
				if (bcc.Trim().Length > 0) msg.Bcc.Add(bcc);
				mailClient.EnableSsl = true;
				mailClient.Credentials = cred;
				mailClient.Send(msg);
			} catch (Exception e) { streamJson(e.Message); } finally {
				try { mailClient.Dispose(); mailClient = null; } catch { }
			}
		}

		#endregion

		#region ######################################################################################################################################################## Internal Classes

		public class Crypto {

			//While an app specific salt is not the best practice for
			//password based encryption, it's probably safe enough as long as
			//it is truly uncommon. Also too much work to alter this answer otherwise.
			private static byte[] _salt = Encoding.UTF8.GetBytes(cryptoSalt);

			/// <summary>
			/// Encrypt the given string using AES.  The string can be decrypted using 
			/// DecryptStringAES().  The sharedSecret parameters must match.
			/// </summary>
			/// <param name="plainText">The text to encrypt.</param>
			/// <param name="sharedSecret">A password used to generate a key for encryption.</param>
			public static string EncryptStringAES(string plainText, string sharedSecret) {
				if (string.IsNullOrEmpty(plainText))
					throw new ArgumentNullException("plainText");
				if (string.IsNullOrEmpty(sharedSecret))
					throw new ArgumentNullException("sharedSecret");

				string outStr = null;                       // Encrypted string to return
				RijndaelManaged aesAlg = null;              // RijndaelManaged object used to encrypt the data.

				try {
					// generate the key from the shared secret and the salt
					Rfc2898DeriveBytes key = new Rfc2898DeriveBytes(sharedSecret, _salt);

					// Create a RijndaelManaged object
					aesAlg = new RijndaelManaged();
					aesAlg.Key = key.GetBytes(aesAlg.KeySize / 8);

					// Create a decryptor to perform the stream transform.
					ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

					// Create the streams used for encryption.
					using (MemoryStream msEncrypt = new MemoryStream()) {
						// prepend the IV
						msEncrypt.Write(BitConverter.GetBytes(aesAlg.IV.Length), 0, sizeof(int));
						msEncrypt.Write(aesAlg.IV, 0, aesAlg.IV.Length);
						using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write)) {
							using (StreamWriter swEncrypt = new StreamWriter(csEncrypt)) {
								//Write all data to the stream.
								swEncrypt.Write(plainText);
							}
						}
						outStr = Convert.ToBase64String(msEncrypt.ToArray());
					}
				} finally {
					// Clear the RijndaelManaged object.
					if (aesAlg != null)
						aesAlg.Clear();
				}

				// Return the encrypted bytes from the memory stream.
				return outStr;
			}

			/// <summary>
			/// Decrypt the given string.  Assumes the string was encrypted using 
			/// EncryptStringAES(), using an identical sharedSecret.
			/// </summary>
			/// <param name="cipherText">The text to decrypt.</param>
			/// <param name="sharedSecret">A password used to generate a key for decryption.</param>
			public static string DecryptStringAES(string cipherText, string sharedSecret) {
				if (string.IsNullOrEmpty(cipherText))
					throw new ArgumentNullException("cipherText");
				if (string.IsNullOrEmpty(sharedSecret))
					throw new ArgumentNullException("sharedSecret");

				// Declare the RijndaelManaged object
				// used to decrypt the data.
				RijndaelManaged aesAlg = null;

				// Declare the string used to hold
				// the decrypted text.
				string plaintext = null;

				try {
					// generate the key from the shared secret and the salt
					Rfc2898DeriveBytes key = new Rfc2898DeriveBytes(sharedSecret, _salt);

					// Create the streams used for decryption.                
					byte[] bytes = Convert.FromBase64String(cipherText);
					using (MemoryStream msDecrypt = new MemoryStream(bytes)) {
						// Create a RijndaelManaged object
						// with the specified key and IV.
						aesAlg = new RijndaelManaged();
						aesAlg.Key = key.GetBytes(aesAlg.KeySize / 8);
						// Get the initialization vector from the encrypted stream
						aesAlg.IV = ReadByteArray(msDecrypt);
						// Create a decrytor to perform the stream transform.
						ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);
						using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read)) {
							using (StreamReader srDecrypt = new StreamReader(csDecrypt))

								// Read the decrypted bytes from the decrypting stream
								// and place them in a string.
								plaintext = srDecrypt.ReadToEnd();
						}
					}
				} finally {
					// Clear the RijndaelManaged object.
					if (aesAlg != null)
						aesAlg.Clear();
				}

				return plaintext;
			}

			private static byte[] ReadByteArray(Stream s) {
				byte[] rawLength = new byte[sizeof(int)];
				if (s.Read(rawLength, 0, rawLength.Length) != rawLength.Length) {
					throw new SystemException("Stream did not contain properly formatted byte array");
				}

				byte[] buffer = new byte[BitConverter.ToInt32(rawLength, 0)];
				if (s.Read(buffer, 0, buffer.Length) != buffer.Length) {
					throw new SystemException("Did not read byte array properly");
				}

				return buffer;
			}
		}



		private class ResponseLogin {
			public int errorCode { get; set; } = 0;
			public string message { get; set; } = "";
			public string method { get; set; } = "";
		}

		private class ResponseError {
			public int errorCode { get; set; } = 0;
			public string message { get; set; } = "";
			public string method { get; set; } = "";
		}

		private class ResponseMessage {
			public string message { get; set; }
		}

		public class PermissionResponse {
			public int errorCode { get; set; } = 0;
			public string message { get; set; } = "You do not have permission to use this service";

			public PermissionResponse() { }

			public PermissionResponse(string message, int errorCode) {
				this.message = message;
				this.errorCode = errorCode;
			}

			public PermissionResponse(string message) { this.message = message; }
			public PermissionResponse(int errorCode) { this.errorCode = errorCode; }

		}

		// simple encryption class...
		public static class SHA {
			public static string GenerateSHA256String(string inputString) {
				SHA256 sha256 = SHA256Managed.Create();
				byte[] bytes = Encoding.UTF8.GetBytes(inputString);
				byte[] hash = sha256.ComputeHash(bytes);
				return GetStringFromHash(hash);
			}

			public static string GenerateSHA512String(string inputString) {
				SHA512 sha512 = SHA512Managed.Create();
				byte[] bytes = Encoding.UTF8.GetBytes(inputString);
				byte[] hash = sha512.ComputeHash(bytes);
				return GetStringFromHash(hash);
			}

			private static string GetStringFromHash(byte[] hash) {
				StringBuilder result = new StringBuilder();
				for (int i = 0; i < hash.Length; i++) {
					result.Append(hash[i].ToString("X2"));
				}
				return result.ToString();
			}

		}
		#endregion
		// ========================================================================================
		//					END - DO NOT CHANGE
		// ========================================================================================


		/* Example of a connection string that points to the AP database on the localdb SQL Server
		  <connectionStrings>
			<add name="DefaultConnection" connectionString="Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=AP;Integrated Security=True;Connect Timeout=30;Encrypt=False;TrustServerCertificate=True" providerName="System.Data.SqlClient" />
		  </connectionStrings>
		*/

		// Methods
		#region ######################################################################################################################################################## Methods
		[WebMethod]
		public void getAllGames()
        {
            send("getAllGames", serializeStyle.DATA_TABLE);
        }

        [WebMethod]
        public void getGameByTitle(string data)
        {
            addParam("@title", data);
            send("getGameByTitle", serializeStyle.DATA_TABLE);
        }
		#endregion

	}
}

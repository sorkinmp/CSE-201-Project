// Need the class topnav to have these buttons if not logged in
// <a href="login.html">Login</a>
// <a href="signUp.html">Sign Up</a>
// <a class="active" href="index.html">Home</a>
$(document).ready(() => {
    // check if there is an email and admin in the url
    var email = getUrlParam('email', 'null');
    var admin = getUrlParam('admin', 'false');
    console.log("Email: " + email);
    console.log("Admin: " + admin);
    // if there is then add the buttons
    if (email == 'null' || admin == 'null') {
        console.log("email: " + email);
        console.log("admin: " + admin);
        var login = "<a href='login.html'>Login</a>";
        var signUp = "<a href='signUp.html'>Sign Up</a>";
        var home = "<a class='active' href='index.html'>Home</a>";
        $(".topnav").html(login + signUp + home);
    } else {
        console.log("emailR: " + email);
        console.log("adminR: " + admin);
        // add the home, account, and a logout button
        var home2 = "<a class='active' href='index.html'>Home</a>";
        var account = "<a href='Account.html'>Account Page</a>";
        var logout = "<a href='index.html'>Logout</a>";
        $(".topnav").html(home2 + account + logout);

    }

}); // end document.ready


// the following functions were found at: https://html-online.com/articles/get-url-parameters-javascript/
// gets the different variables separated by '&' in the URL
// example use: var number = getUrlVars()["x"];
function getUrlVars() {
    var vars = {};
    var parts = window.location.href.replace(/[?&]+([^=&]+)=([^&]*)/gi, function (m, key, value) {
        vars[key] = value;
    });
    return vars;
}

// allows a default value to be set, if the parameter exists then the value will be
// changed to that, otherwise the defaut value will be returned
// example use: var mytext = getUrlParam('text','Empty');
function getUrlParam(parameter, defaultvalue) {
    var urlparameter = defaultvalue;
    if (window.location.href.indexOf(parameter) > -1) {
        urlparameter = getUrlVars()[parameter];
    }
    return urlparameter;
}

var DomainID = null;
function getDomainIDs()
{
    $.ajax
        ({
            type: "POST",
            url: "/List/GetDomainIDs",
            contentType: "application/json; charset=utf-8",
            datatype: 'json',
            cache: false,
            async: true,
            success: function (data) {
                console.log("DomainIDs=" + data);
                DomainID = data;
            },
            error: function (data) {
                //code
            }
        });
}


function validateEmailID(emailId) {
    console.log("email in js=" + emailId);
    var validRegex = null;
    var EmailValidate = '@System.Configuration.ConfigurationManager.AppSettings["EmailValidate"]';

    if (EmailValidate == "mobinext") {
       // validRegex = /^[a-zA-Z0-9.!#$%&'*+/=?^_`{|}~-]+@("@mobinexttech.com")$/;

        var dynamicPart = "@mobinexttech.com";
        var validRegex = new RegExp("/^[a-zA-Z0-9.!#$%&'*+/=?^_`{|}~-]+@" + dynamicPart + "$/");
    }
    else if (EmailValidate == "skoda") {
        var validRegex = /^[a-zA-Z0-9.!#$%&'*+/=?^_`{|}~-]+@("@skoda-vw.co.in")$/;
    }

    var receivedEmail = emailId;
    //console.log("receivedEmail="+receivedEmail);
    if (!(receivedEmail.toLowerCase().match(validRegex))) {
    //if (validRegex.test(emailId)) {
        return false;
    }
    else
        return true;

}



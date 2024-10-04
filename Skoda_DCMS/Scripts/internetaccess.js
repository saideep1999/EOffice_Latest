function submitInternetAccessRequestDetails() {
    //
    saveAndSubmitInternetAccessRequestDetails();
    //$("#dialog-confirm").dialog({
    //    resizable: false,
    //    height: "auto",
    //    width: 400,
    //    modal: true,
    //    buttons: {
    //        "Yes": function () {
    //            $(this).dialog("close");

    //            //Collect all form details and save it to DB using MVC controller action
    //            saveAndSubmitInternetAccessRequestDetails();
    //        },

    //        Cancel: function () {
    //            $(this).dialog("close");
    //        }
    //    }
    //});
}

function saveAndSubmitInternetAccessRequestDetails() {
    //
    //Gather all the input data and send it to controller.
    var internetAccessRequestDto = {};
    internetAccessRequestDto = getSubmitterUIData(internetAccessRequestDto);
    console.log(internetAccessRequestDto.EmployeeContactNo);
    console.log($('#txtContactNo').val());
    internetAccessRequestDto.EmployeeRequestType = $("input[name='EmployeeRequestType']:checked").val();
    internetAccessRequestDto.IsSpecialRequest = $("input[name='specialRequest']:checked").val() == "true" ? true : false;
    internetAccessRequestDto.MoreInformation = $('#txtMoreInformation').val();


    if ($("input[name='TypeOfEmployee']:checked").val() == 'External') {
        internetAccessRequestDto.EmployeeOrg = $('#ddEmpOrganization :selected').val();
    }

    if ($("input[name='EmployeeRequestType']:checked").val() == 'Temporary') {
        internetAccessRequestDto.TempFrom = $('#txtTempFrom').val();
        internetAccessRequestDto.TempTo = $('#txtTempTo').val();
    }

    internetAccessRequestDto.BusinessNeed = $('#txtBusinessNeed').val();

    //Other Employee Information
    internetAccessRequestDto = getOnBehalfEmpUIData(internetAccessRequestDto);
    console.log(internetAccessRequestDto.OtherEmployeePhone);
    //console.log(internetAccessRequestDto);
    $.ajax({
        type: "POST",
        url: "/List/CreateInternetAccessRequest",
        data: '{internetAccessRequestDto: ' + JSON.stringify(internetAccessRequestDto) + '}',
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        cache: false,
        async: false,
        success: function (response) {
            console.log(response);
            if (response != false) {
                $('#btnSubmit').hide();
                $("#IAForm").trigger("reset");
                //redirectUser();
                //$('#errorSummaryItems').empty();
                //$("#errorSummary").hide();

                //$("#dialog-message").dialog({
                //    modal: true,
                //    buttons: {
                //        Ok: function () {
                //            $(this).dialog("close");
                //            redirectUser();
                //        }
                //    }
                //});
                successModal();
            }
            else {
                showErrorSummary(response);
            }
        },

        failure: function (response) {
            //
            alert(response.responseText);
        },

        error: function (response) {
            showErrorSummary(response);
        }
    });
}

function cancelInternetAccessRequestDetails() {
    window.location.href = '/Dashboard';
}

function approveRequest(appRowId) {
    //
    //var requestDtoList = createRequestDtoList($('#lblVWMasterRequestId').val(),
    //    $('#lblInternetAccessRequestId').val(),
    var comments = $('#txtRejectComments').val();

    approveSelectedRequest(comments, appRowId);
}

function rejectRequest(appRowId) {
    //
    //var requestDtoList = createRequestDtoList($('#lblVWMasterRequestId').val(),
    //    $('#lblInternetAccessRequestId').val(),
    var comments = $('#txtRejectComments').val();

    //Check if comments are missing.   
    var commentsMissing = checkComments(requestDtoList[0].Comments);
    if (commentsMissing == true) {
        return;
    }
    else {
        $('#errorSummaryItems').empty();
        $("#errorSummary").hide();
    }

    rejectSelectedRequest(comments, appRowId);


}
function successModal() {
    $('#modalTitle').html('Success');
    $('#modalBody').html('Your form has been submitted!');
    $("#modalOkButton").attr('data-dismiss', 'modal');
    $("#successModel").modal('show');
}
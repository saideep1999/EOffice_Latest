function AddError(id, className = '', errorId = 0) {
    var tagName = $("#" + id).get(0).tagName
    let isReadOnly = $("#" + id).is('[readonly]');
    let isDisabled = $("#" + id).is(':disabled');

    if (tagName == 'SELECT') {
        if (!isReadOnly && !isDisabled)
            $("#" + id).addClass("errorIconDropDown");

        $("#" + id).on('change', function () {
            console.log($(this).val());
            if ($(this).val() != '' && $(this).val() != '0' && $(this).val() != 0 && $(this).val() != null && $(this).val() != undefined) {
                $("#" + id).removeClass("errorIconDropDown");
                $("#errorList li[id='error" + errorId + "']").remove();
                if ($("#errorList li").length == 0) {
                    $("#headingErr").addClass('d-none');
                    document.getElementById("add_to_me").innerHTML = '';
                }
                else {
                    $("#headingErr").removeClass('d-none');
                }
                $("#" + id).off('change');
            }
        });
    }
    else if (tagName == 'INPUT') {
        var inputs = $("#" + id).attr('type');
        if (inputs == 'date') {
            if (!isReadOnly && !isDisabled)
                $("#" + id).addClass("errorIconTypeDate");

            $("#" + id).on('change', function () {
                console.log($(this).val());
                if ($(this).val()) {
                    $("#" + id).removeClass("errorIconTypeDate");
                    $("#errorList li[id='error" + errorId + "']").remove();
                    if ($("#errorList li").length == 0) {
                        $("#headingErr").addClass('d-none');
                        document.getElementById("add_to_me").innerHTML = '';
                    }
                    else {
                        $("#headingErr").removeClass('d-none');
                    }
                    $("#" + id).off('change');
                }
            });
        }
        else {
            if (!isReadOnly && !isDisabled)
                $("#" + id).addClass("errorIcon");
            $("#" + id).on('change', function () {
                console.log($(this).val());
                if ($(this).val()) {
                    $("#" + id).removeClass("errorIcon");
                    $("#errorList li[id='error" + errorId + "']").remove();
                    if ($("#errorList li").length == 0) {
                        $("#headingErr").addClass('d-none');
                        document.getElementById("add_to_me").innerHTML = '';
                    }
                    else {
                        $("#headingErr").removeClass('d-none');
                    }
                    $("#" + id).off('change');
                }
            });
        }

    }
    else if (tagName == 'TEXTAREA') {
        if (!isReadOnly && !isDisabled)
            $("#" + id).addClass("errorIconTextarea");

        $("#" + id).on('change', function () {
            console.log($(this).val());
            if ($(this).val()) {
                $("#" + id).removeClass("errorIconTextarea");
                $("#errorList li[id='error" + errorId + "']").remove();
                if ($("#errorList li").length == 0) {
                    $("#headingErr").addClass('d-none');
                    document.getElementById("add_to_me").innerHTML = '';
                }
                else {
                    $("#headingErr").removeClass('d-none');
                }
                $("#" + id).off('change');
            }
        });
    }
    else if (tagName == 'LABEL') {
        $("#" + id).addClass("errorIconLbl");
        let enableCount = 0;
        $("." + className).each(function () {
            console.log($(this).attr('id'));
            console.log($(this).is(':disabled'));
            if (!$(this).is(':disabled')) {
                enableCount++;
            }
        })

        if (enableCount > 0) {
            $("." + className).on('change', function () {
                console.log($(this).val());
                $("#" + id).removeClass("errorIconLbl");
                $("#errorList li[id='error" + errorId + "']").remove();
                if ($("#errorList li").length == 0) {
                    $("#headingErr").addClass('d-none');
                    document.getElementById("add_to_me").innerHTML = '';
                }
                else {
                    $("#headingErr").removeClass('d-none');
                }
                $("#" + id).off('change');
            });
        }
    }
}

function RemoveError(id) {
    //var tagName = $("#" + id).get(0).tagName
    var tagName = $("#" + id).get(0).tagName
    if (tagName == 'SELECT') {
        $("#" + id).removeClass("errorIconDropDown");
    }
    else if (tagName == 'INPUT') {
        $("#" + id).removeClass("errorIcon");
    }
    else if (tagName == 'TEXTAREA') {
        $("#" + id).removeClass("errorIconTextarea");
    }
    else if (tagName == 'LABEL') {
        $("#" + id).removeClass("errorIconLbl");
    }
}





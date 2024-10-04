////function AddFocusEvent() {
////    var focusEvent = function () {
////        var nextTag = this.nextElementSibling;
////        if (nextTag && nextTag.tagName.toLowerCase() === "span" && nextTag.classList.contains("placeholder"))
////            nextTag.classList.add("spanUpperSide");
////    };
////    var focusLostEvent = function () {
////        var nextTag = this.nextElementSibling;
////        console.log(this.nodeValue);
////        if (nextTag && nextTag.tagName.toLowerCase() === "span" && nextTag.classList.contains("placeholder") && !this.nodeValue)
////            nextTag.classList.remove("spanUpperSide");
////    };
////    $('input[type="text"]').focus(focusEvent);
////    $('input[type="text"]').focusout(focusLostEvent);
////    $('textarea').focus(focusEvent);
////    $('textarea').focusout(focusLostEvent);

////    var changeEvent = function() {
////        if (this.nodeValue)
////            nextTag.classList.remove("spanUpperSide");
////        else
////            nextTag.classList.add("spanUpperSide");
////    }
////    $('input[type="text"]').change(changeEvent);
////    $('textarea').change(changeEvent);
////}
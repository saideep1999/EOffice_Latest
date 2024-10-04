
/** ******  left menu  *********************** **/

menu();
    $(window).resize(menu);
    
    function menu(){
        var win = $(window).width();
        if(win < 991){
            $('body').removeClass('nav-sm');
            $('body').addClass('nav-md');
        }
        else{
            $('body').removeClass('nav-md');
            $('body').addClass('nav-sm');
            
        }
    }
$(function () {
    $('.close_nav').click(function(){
        $('body').toggleClass('nav-sm nav-md');
    });
    $('#sidebar-menu li ul').slideUp();
    $('#sidebar-menu li').removeClass('active');
    $('#sidebar-menu li').on('click', function() {
        var link = $('a', this).attr('href');
        if(link) { 
            window.location.href = link;
        } else {
            if ($(this).is('.active')) {
                $(this).removeClass('active');
                $('ul', this).slideUp();
            } else {
                $('#sidebar-menu li').removeClass('active');
                $('#sidebar-menu li ul').slideUp();
                
                $(this).addClass('active');
                $('ul', this).slideDown();
            }
        }
    });

    $('#menu_toggle').click(function () {
        if ($('body').hasClass('nav-md')) {
            $('body').removeClass('nav-md').addClass('nav-sm');
            $('.left_col').removeClass('scroll-view').removeAttr('style');
            $('.sidebar-footer').hide();

            if ($('#sidebar-menu li').hasClass('active')) {
                $('#sidebar-menu li.active').addClass('active-sm').removeClass('active');
            }
        } else {
            $('body').removeClass('nav-sm').addClass('nav-md');
            $('.sidebar-footer').show();
            if ($('#sidebar-menu li').hasClass('active-sm')) {
                $('#sidebar-menu li.active-sm').addClass('active').removeClass('active-sm');
            }
        }
    });

});



/* Sidebar Menu active class */
$(function () {
    var url = window.location;
    $('#sidebar-menu a[href="' + url + '"]').parent('li').addClass('current-page');
    $('#sidebar-menu a').filter(function () {
        return this.href == url;
    }).parent('li').addClass('current-page').parent('ul').slideDown().parent().addClass('active');
});

/** ******  /left menu  *********************** **/
/** ******  right_col height flexible  *********************** **/
//$(".right_col").css("min-height", $(window).height());
//$(window).resize(function () {
    //$(".right_col").css("min-height", $(window).height());
//});
/** ******  /right_col height flexible  *********************** **/



/** ******  iswitch  *********************** **/
if ($("input.flat")[0]) {
    $(document).ready(function () {
        $('input.flat').iCheck({
            checkboxClass: 'icheckbox_flat-green',
            radioClass: 'iradio_flat'
        });
    });
}
/** ******  /iswitch  *********************** **/


$('.itBestPractices').owlCarousel({
    loop: false,
    margin:10,
    nav:true,
    dots:false,
    responsive:{
        0:{
            items:2
        },
        767:{
            items:1
        },
        1000:{
            items:2
        }
    }
})
$('.busipract').owlCarousel({
    loop: false,
    margin:10,
    nav:true,
    dots:false,
    responsive:{
        0:{
            items:2
        },
        767:{
            items:1
        },
        1000:{
            items:2
        }
    }
})
$('.standards_group').owlCarousel({
    loop:true,
    margin:10,
    nav:true,
    dots:false,
    responsive:{
        0:{
            items:2
        },
        767:{
            items:1
        },
        1000:{
            items:2
        }
    }
})


$('.news-ticker').owlCarousel({
    loop:true,
    margin:10,
    nav:true,
    dots:false,    
    responsive:{
        0:{
            items:1
        },
        600:{
            items:1
        },
        1000:{
            items:1
        }
    }
})


$(function () {
    $('#file').change(function () {
        var i = $(this).prev('label').clone();
        var file = $('#file')[0].files[0].name;
        $(this).next('.uploadfile').show().text(file);
    });
});
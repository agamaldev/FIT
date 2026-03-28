// Banner-Slider-1
$(document).ready(function () {
    $(".banner_slider_list.lazy").slick({
        dots: false,
        arrows: true,
        infinite: true,
        slidesToShow: 1,
        slidesToScroll: 1,
        lazyLoad: "progressive",
        cssEase: "linear",
        fade: true,
        autoplay: true,
        autoplaySpeed: 4000,
        nextArrow:
        '<div class="slick-custom-arrow slick-custom-arrow-right orangeglow"><svg width="60" height="60" viewBox="0 0 60 60" fill="none" xmlns="http://www.w3.org/2000/svg"><mask id="mask0_1_13599" style="mask-type:alpha" maskUnits="userSpaceOnUse" x="0" y="0" width="60" height="60"><path d="M0 0H60V60H0V0Z" fill="white"/></mask><g mask="url #mask0_1_13599)"><path d="M12.5 30H47.5" stroke="black" stroke-width="3" stroke-linecap="round" stroke-linejoin="round"/><path d="M37.5 40L47.5 30" stroke="black" stroke-width="3" stroke-linecap="round" stroke-linejoin="round"/><path d="M37.5 20L47.5 30" stroke="black" stroke-width="3" troke-linecap="round" stroke-linejoin="round"/></g></svg></div>',
        prevArrow:
        '<div class="slick-custom-arrow slick-custom-arrow-left orangeglow"><svg width="60" height="60" viewBox="0 0 60 60" fill="none" xmlns="http://www.w3.org/2000/svg"><mask id="mask0_1_13607" style="mask-type:alpha" maskUnits="userSpaceOnUse" x="0" y="0" width="60" height="60"><path d="M0 0H60V60H0V0Z" fill="white"/></mask><g mask="url(#mask0_1_13607)"><path d="M12.5 30H47.5" stroke="black" stroke-width="3" stroke-linecap="round" stroke-linejoin="round"/><path d="M12.5 30L22.5 40" stroke="black" stroke-width="3" stroke-linecap="round" stroke-linejoin="round"/><path d="M12.5 30L22.5 20" stroke="black" stroke-width="3" stroke-linecap="round" stroke-linejoin="round"/></g></svg></div>',    
    }).slickAnimation();

    $('.slick-nav').on('click touch', function(e) {
        e.preventDefault();
        
        var arrow = $(this);
        
        if(!arrow.hasClass('animate')) {
            arrow.addClass('animate');
            setTimeout(() => {
                arrow.removeClass('animate');
            }, 1600);
        }
    });
    // Banner-Slider-2
    $(".banner_slider_list_area.lazy").slick({
        dots: false,
        arrows: true,
        infinite: true,
        slidesToShow: 1,
        slidesToScroll: 1,
        lazyLoad: "progressive",
        cssEase: "linear",
        fade: true,
        autoplay: true,
        autoplaySpeed: 4000,
        nextArrow:
        '<div class="slick-custom-arrow slick-custom-arrow-right orangeglow"><svg width="60" height="60" viewBox="0 0 60 60" fill="none" xmlns="http://www.w3.org/2000/svg"><mask id="mask0_1_13599" style="mask-type:alpha" maskUnits="userSpaceOnUse" x="0" y="0" width="60" height="60"><path d="M0 0H60V60H0V0Z" fill="white"/></mask><g mask="url #mask0_1_13599)"><path d="M12.5 30H47.5" stroke="black" stroke-width="3" stroke-linecap="round" stroke-linejoin="round"/><path d="M37.5 40L47.5 30" stroke="black" stroke-width="3" stroke-linecap="round" stroke-linejoin="round"/><path d="M37.5 20L47.5 30" stroke="black" stroke-width="3" troke-linecap="round" stroke-linejoin="round"/></g></svg></div>',
        prevArrow:
        '<div class="slick-custom-arrow slick-custom-arrow-left orangeglow"><svg width="60" height="60" viewBox="0 0 60 60" fill="none" xmlns="http://www.w3.org/2000/svg"><mask id="mask0_1_13607" style="mask-type:alpha" maskUnits="userSpaceOnUse" x="0" y="0" width="60" height="60"><path d="M0 0H60V60H0V0Z" fill="white"/></mask><g mask="url(#mask0_1_13607)"><path d="M12.5 30H47.5" stroke="black" stroke-width="3" stroke-linecap="round" stroke-linejoin="round"/><path d="M12.5 30L22.5 40" stroke="black" stroke-width="3" stroke-linecap="round" stroke-linejoin="round"/><path d="M12.5 30L22.5 20" stroke="black" stroke-width="3" stroke-linecap="round" stroke-linejoin="round"/></g></svg></div>',    
    }).slickAnimation();

    $('.slick-nav').on('click touch', function(e) {
        e.preventDefault();
        
        var arrow = $(this);
        
        if(!arrow.hasClass('animate')) {
            arrow.addClass('animate');
            setTimeout(() => {
                arrow.removeClass('animate');
            }, 1600);
        }
    });
    // Banner-Slider-4
    $(".yoga_slider_list").slick({
        dots: false,
        arrows: true,
        infinite: true,
        slidesToShow: 1,
        slidesToScroll: 1,
        lazyLoad: "progressive",
        cssEase: "linear",
        fade: true,
        // autoplay: true,
        // autoplaySpeed: 4000,
        nextArrow:
        '<div class="slick-custom-arrow slick-custom-arrow-right orangeglow"><svg width="60" height="60" viewBox="0 0 60 60" fill="none" xmlns="http://www.w3.org/2000/svg"><mask id="mask0_1_13599" style="mask-type:alpha" maskUnits="userSpaceOnUse" x="0" y="0" width="60" height="60"><path d="M0 0H60V60H0V0Z" fill="white"/></mask><g mask="url #mask0_1_13599)"><path d="M12.5 30H47.5" stroke="black" stroke-width="3" stroke-linecap="round" stroke-linejoin="round"/><path d="M37.5 40L47.5 30" stroke="black" stroke-width="3" stroke-linecap="round" stroke-linejoin="round"/><path d="M37.5 20L47.5 30" stroke="black" stroke-width="3" troke-linecap="round" stroke-linejoin="round"/></g></svg></div>',
        prevArrow:
        '<div class="slick-custom-arrow slick-custom-arrow-left orangeglow"><svg width="60" height="60" viewBox="0 0 60 60" fill="none" xmlns="http://www.w3.org/2000/svg"><mask id="mask0_1_13607" style="mask-type:alpha" maskUnits="userSpaceOnUse" x="0" y="0" width="60" height="60"><path d="M0 0H60V60H0V0Z" fill="white"/></mask><g mask="url(#mask0_1_13607)"><path d="M12.5 30H47.5" stroke="black" stroke-width="3" stroke-linecap="round" stroke-linejoin="round"/><path d="M12.5 30L22.5 40" stroke="black" stroke-width="3" stroke-linecap="round" stroke-linejoin="round"/><path d="M12.5 30L22.5 20" stroke="black" stroke-width="3" stroke-linecap="round" stroke-linejoin="round"/></g></svg></div>',    
    }).slickAnimation();

    $('.slick-nav').on('click touch', function(e) {
        e.preventDefault();
        
        var arrow = $(this);
        
        if(!arrow.hasClass('animate')) {
            arrow.addClass('animate');
            setTimeout(() => {
                arrow.removeClass('animate');
            }, 1600);
        }
    });
    // Client-Say Slider V1
    $(".client_say_slider_list.lazy").slick({
        dots: false,
        arrows: true,
        infinite: true,
        speed: 900,
        slidesToShow: 1,
        slidesToScroll: 1,
        lazyLoad: "progressive",
        cssEase: "linear",
        fade: true,
        autoplay: true,
        autoplaySpeed: 3000,
        nextArrow:
        '<div class="slick-custom-arrow slick-custom-arrow-right orangeglow"><svg width="60" height="60" viewBox="0 0 60 60" fill="none" xmlns="http://www.w3.org/2000/svg"><mask id="mask0_1_13599" style="mask-type:alpha" maskUnits="userSpaceOnUse" x="0" y="0" width="60" height="60"><path d="M0 0H60V60H0V0Z" fill="white"/></mask><g mask="url #mask0_1_13599)"><path d="M12.5 30H47.5" stroke="black" stroke-width="3" stroke-linecap="round" stroke-linejoin="round"/><path d="M37.5 40L47.5 30" stroke="black" stroke-width="3" stroke-linecap="round" stroke-linejoin="round"/><path d="M37.5 20L47.5 30" stroke="black" stroke-width="3" troke-linecap="round" stroke-linejoin="round"/></g></svg></div>',
        prevArrow:
        '<div class="slick-custom-arrow slick-custom-arrow-left orangeglow"><svg width="60" height="60" viewBox="0 0 60 60" fill="none" xmlns="http://www.w3.org/2000/svg"><mask id="mask0_1_13607" style="mask-type:alpha" maskUnits="userSpaceOnUse" x="0" y="0" width="60" height="60"><path d="M0 0H60V60H0V0Z" fill="white"/></mask><g mask="url(#mask0_1_13607)"><path d="M12.5 30H47.5" stroke="black" stroke-width="3" stroke-linecap="round" stroke-linejoin="round"/><path d="M12.5 30L22.5 40" stroke="black" stroke-width="3" stroke-linecap="round" stroke-linejoin="round"/><path d="M12.5 30L22.5 20" stroke="black" stroke-width="3" stroke-linecap="round" stroke-linejoin="round"/></g></svg></div>',    
    }).slickAnimation();

    $('.slick-nav').on('click touch', function(e) {
        e.preventDefault();
        
        var arrow = $(this);
        
        if(!arrow.hasClass('animate')) {
            arrow.addClass('animate');
            setTimeout(() => {
                arrow.removeClass('animate');
            }, 1600);
        }
    });

    // Client-Say Slider V2
    $(".client_say_slider_v2_list.lazy").slick({
        dots: false,
        arrows: true,
        infinite: true,
        speed: 900,
        slidesToShow: 1,
        slidesToScroll: 1,
        lazyLoad: "progressive",
        cssEase: "linear",
        fade: true,
        autoplay: true,
        autoplaySpeed: 3000,
        nextArrow:
        '<div class="slick-custom-arrow slick-custom-arrow-right orangeglow"><svg width="60" height="60" viewBox="0 0 60 60" fill="none" xmlns="http://www.w3.org/2000/svg"><mask id="mask0_1_13599" style="mask-type:alpha" maskUnits="userSpaceOnUse" x="0" y="0" width="60" height="60"><path d="M0 0H60V60H0V0Z" fill="white"/></mask><g mask="url #mask0_1_13599)"><path d="M12.5 30H47.5" stroke="black" stroke-width="3" stroke-linecap="round" stroke-linejoin="round"/><path d="M37.5 40L47.5 30" stroke="black" stroke-width="3" stroke-linecap="round" stroke-linejoin="round"/><path d="M37.5 20L47.5 30" stroke="black" stroke-width="3" troke-linecap="round" stroke-linejoin="round"/></g></svg></div>',
        prevArrow:
        '<div class="slick-custom-arrow slick-custom-arrow-left orangeglow"><svg width="60" height="60" viewBox="0 0 60 60" fill="none" xmlns="http://www.w3.org/2000/svg"><mask id="mask0_1_13607" style="mask-type:alpha" maskUnits="userSpaceOnUse" x="0" y="0" width="60" height="60"><path d="M0 0H60V60H0V0Z" fill="white"/></mask><g mask="url(#mask0_1_13607)"><path d="M12.5 30H47.5" stroke="black" stroke-width="3" stroke-linecap="round" stroke-linejoin="round"/><path d="M12.5 30L22.5 40" stroke="black" stroke-width="3" stroke-linecap="round" stroke-linejoin="round"/><path d="M12.5 30L22.5 20" stroke="black" stroke-width="3" stroke-linecap="round" stroke-linejoin="round"/></g></svg></div>',    
    }).slickAnimation();

    $('.slick-nav').on('click touch', function(e) {
        e.preventDefault();
        
        var arrow = $(this);
        
        if(!arrow.hasClass('animate')) {
            arrow.addClass('animate');
            setTimeout(() => {
                arrow.removeClass('animate');
            }, 1600);
        }
    });
    // Client-Say Slider V3
    $(".client_say_slider_list_v3").slick({
        dots: false,
        arrows: true,
        infinite: true,
        speed: 400,
        slidesToShow: 1,
        slidesToScroll: 1,
        cssEase: "linear",
        fade: true,
        // autoplay: true,
        // autoplaySpeed: 3000,
        nextArrow:
        '<div class="slick-custom-arrow slick-custom-arrow-right orangeglow"><svg width="60" height="60" viewBox="0 0 60 60" fill="none" xmlns="http://www.w3.org/2000/svg"><mask id="mask0_1_13599" style="mask-type:alpha" maskUnits="userSpaceOnUse" x="0" y="0" width="60" height="60"><path d="M0 0H60V60H0V0Z" fill="white"/></mask><g mask="url #mask0_1_13599)"><path d="M12.5 30H47.5" stroke="black" stroke-width="3" stroke-linecap="round" stroke-linejoin="round"/><path d="M37.5 40L47.5 30" stroke="black" stroke-width="3" stroke-linecap="round" stroke-linejoin="round"/><path d="M37.5 20L47.5 30" stroke="black" stroke-width="3" troke-linecap="round" stroke-linejoin="round"/></g></svg></div>',
        prevArrow:
        '<div class="slick-custom-arrow slick-custom-arrow-left orangeglow"><svg width="60" height="60" viewBox="0 0 60 60" fill="none" xmlns="http://www.w3.org/2000/svg"><mask id="mask0_1_13607" style="mask-type:alpha" maskUnits="userSpaceOnUse" x="0" y="0" width="60" height="60"><path d="M0 0H60V60H0V0Z" fill="white"/></mask><g mask="url(#mask0_1_13607)"><path d="M12.5 30H47.5" stroke="black" stroke-width="3" stroke-linecap="round" stroke-linejoin="round"/><path d="M12.5 30L22.5 40" stroke="black" stroke-width="3" stroke-linecap="round" stroke-linejoin="round"/><path d="M12.5 30L22.5 20" stroke="black" stroke-width="3" stroke-linecap="round" stroke-linejoin="round"/></g></svg></div>',    
    }).slickAnimation();

    $('.slick-nav').on('click touch', function(e) {
        e.preventDefault();
        
        var arrow = $(this);
        
        if(!arrow.hasClass('animate')) {
            arrow.addClass('animate');
            setTimeout(() => {
                arrow.removeClass('animate');
            }, 1600);
        }
    });
    // Client-Say Slider V4
    $(".client_say_slider_list_v4.lazy").slick({
        dots: true,
        arrows: false,
        infinite: true,
        speed: 900,
        slidesToShow: 1,
        slidesToScroll: 1,
        lazyLoad: "progressive",
        cssEase: "linear",
        autoplay: true,
        autoplaySpeed: 3000,
    });
    // Our-Project Slider 1
    $(".individualtraning_single_project_slider_area").slick({
        dots: false,
        arrows: true,
        infinite: true,
        speed: 900,
        slidesToShow: 1,
        slidesToScroll: 1,
        lazyLoad: "progressive",
        cssEase: "linear",
        autoplay: true,
        autoplaySpeed: 3000,
        nextArrow:
        '<div class="slick-custom-arrow slick-custom-arrow-right orangeglow"><svg width="60" height="60" viewBox="0 0 60 60" fill="none" xmlns="http://www.w3.org/2000/svg"><mask id="mask0_1_13599" style="mask-type:alpha" maskUnits="userSpaceOnUse" x="0" y="0" width="60" height="60"><path d="M0 0H60V60H0V0Z" fill="white"/></mask><g mask="url #mask0_1_13599)"><path d="M12.5 30H47.5" stroke="black" stroke-width="3" stroke-linecap="round" stroke-linejoin="round"/><path d="M37.5 40L47.5 30" stroke="black" stroke-width="3" stroke-linecap="round" stroke-linejoin="round"/><path d="M37.5 20L47.5 30" stroke="black" stroke-width="3" troke-linecap="round" stroke-linejoin="round"/></g></svg></div>',
        prevArrow:
        '<div class="slick-custom-arrow slick-custom-arrow-left orangeglow"><svg width="60" height="60" viewBox="0 0 60 60" fill="none" xmlns="http://www.w3.org/2000/svg"><mask id="mask0_1_13607" style="mask-type:alpha" maskUnits="userSpaceOnUse" x="0" y="0" width="60" height="60"><path d="M0 0H60V60H0V0Z" fill="white"/></mask><g mask="url(#mask0_1_13607)"><path d="M12.5 30H47.5" stroke="black" stroke-width="3" stroke-linecap="round" stroke-linejoin="round"/><path d="M12.5 30L22.5 40" stroke="black" stroke-width="3" stroke-linecap="round" stroke-linejoin="round"/><path d="M12.5 30L22.5 20" stroke="black" stroke-width="3" stroke-linecap="round" stroke-linejoin="round"/></g></svg></div>', 
    });
    // Our-Blogs Slider
    $(".Our_blog_box_slider_area").slick({
        dots: false,
        arrows: true,
        infinite: true,
        speed: 900,
        slidesToShow: 1,
        slidesToScroll: 1,
        lazyLoad: "progressive",
        cssEase: "linear",
        autoplay: true,
        autoplaySpeed: 3000,
        nextArrow:
        '<div class="slick-custom-arrow slick-custom-arrow-right orangeglow"><svg width="60" height="60" viewBox="0 0 60 60" fill="none" xmlns="http://www.w3.org/2000/svg"><mask id="mask0_1_13599" style="mask-type:alpha" maskUnits="userSpaceOnUse" x="0" y="0" width="60" height="60"><path d="M0 0H60V60H0V0Z" fill="white"/></mask><g mask="url #mask0_1_13599)"><path d="M12.5 30H47.5" stroke="black" stroke-width="3" stroke-linecap="round" stroke-linejoin="round"/><path d="M37.5 40L47.5 30" stroke="black" stroke-width="3" stroke-linecap="round" stroke-linejoin="round"/><path d="M37.5 20L47.5 30" stroke="black" stroke-width="3" troke-linecap="round" stroke-linejoin="round"/></g></svg></div>',
        prevArrow:
        '<div class="slick-custom-arrow slick-custom-arrow-left orangeglow"><svg width="60" height="60" viewBox="0 0 60 60" fill="none" xmlns="http://www.w3.org/2000/svg"><mask id="mask0_1_13607" style="mask-type:alpha" maskUnits="userSpaceOnUse" x="0" y="0" width="60" height="60"><path d="M0 0H60V60H0V0Z" fill="white"/></mask><g mask="url(#mask0_1_13607)"><path d="M12.5 30H47.5" stroke="black" stroke-width="3" stroke-linecap="round" stroke-linejoin="round"/><path d="M12.5 30L22.5 40" stroke="black" stroke-width="3" stroke-linecap="round" stroke-linejoin="round"/><path d="M12.5 30L22.5 20" stroke="black" stroke-width="3" stroke-linecap="round" stroke-linejoin="round"/></g></svg></div>', 
    });
});
// Meet-Team Slider V1
$(document).ready(function () {
    $('.traning_slider_area').slick({
        dots: true,
        arrows: false,
        infinite: true,
        centerMode: true,
        centerPadding: '0px',
        speed: 300,
        slidesToShow: 3,
        slidesToScroll: 1,   
        autoplay: true,
        autoplaySpeed: 2000, 
        responsive: [
            {
                breakpoint: 990,
                settings: {
                  slidesToShow: 2,
                  centerMode: false,
                }
            },
            {
                breakpoint: 575,
                settings: {
                  slidesToShow: 1,
                  centerMode: false,
                }
            }
        ]
    });
    // Meet-Team Slider V2
    $('.meet_team_v2_slider_list').slick({
        dots: false,
        arrows: true,
        infinite: true,
        speed: 300,
        slidesToShow: 3,
        slidesToScroll: 1,
        autoplay: true,
        autoplaySpeed: 2000,
        nextArrow:
        '<div class="slick-custom-arrow slick-custom-arrow-left orangeglow"><svg width="60" height="60" viewBox="0 0 60 60" fill="none" xmlns="http://www.w3.org/2000/svg"><circle cx="30" cy="30" r="30" fill="black"/><mask id="mask0_442_2" style="mask-type:alpha" maskUnits="userSpaceOnUse" x="18" y="18" width="24" height="24"><path d="M18 18H42V42H18V18Z" fill="white"/></mask><g mask="url(#mask0_442_2)"><path d="M27 36L33 30L27 24" stroke="white" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/></g></svg></div>',
        prevArrow:
        '<div class="slick-custom-arrow slick-custom-arrow-right orangeglow"><svg width="60" height="60" viewBox="0 0 60 60" fill="none" xmlns="http://www.w3.org/2000/svg"><circle cx="30" cy="30" r="30" fill="black"/><mask id="mask0_1_10554" style="mask-type:alpha" maskUnits="userSpaceOnUse" x="18" y="18" width="24" height="24"><path d="M18 18H42V42H18V18Z" fill="white"/></mask><g mask="url(#mask0_1_10554)"><path d="M33 24L27 30L33 36" stroke="white" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/></g></svg></div>',
        responsive: [
            {
                breakpoint: 1100,
                settings: {
                  slidesToShow: 3,
                }
            },
            {
                breakpoint: 990,
                settings: {
                  slidesToShow: 2,
                }
            },
            {
                breakpoint: 576,
                settings: {
                  slidesToShow: 1,
                }
            }
        ]
    });
    $('.meet_team_v2_slider_list_meetteampage').slick({
        dots: false,
        arrows: true,
        infinite: true,
        speed: 300,
        centerMode: true,
        centerPadding: '0px',
        slidesToShow: 3,
        slidesToScroll: 1,
        autoplay: true,
        autoplaySpeed: 2000,
        nextArrow:
        '<div class="slick-custom-arrow slick-custom-arrow-left orangeglow"><svg width="60" height="60" viewBox="0 0 60 60" fill="none" xmlns="http://www.w3.org/2000/svg"><circle cx="30" cy="30" r="30" fill="black"/><mask id="mask0_442_2" style="mask-type:alpha" maskUnits="userSpaceOnUse" x="18" y="18" width="24" height="24"><path d="M18 18H42V42H18V18Z" fill="white"/></mask><g mask="url(#mask0_442_2)"><path d="M27 36L33 30L27 24" stroke="white" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/></g></svg></div>',
        prevArrow:
        '<div class="slick-custom-arrow slick-custom-arrow-right orangeglow"><svg width="60" height="60" viewBox="0 0 60 60" fill="none" xmlns="http://www.w3.org/2000/svg"><circle cx="30" cy="30" r="30" fill="black"/><mask id="mask0_1_10554" style="mask-type:alpha" maskUnits="userSpaceOnUse" x="18" y="18" width="24" height="24"><path d="M18 18H42V42H18V18Z" fill="white"/></mask><g mask="url(#mask0_1_10554)"><path d="M33 24L27 30L33 36" stroke="white" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/></g></svg></div>',
        responsive: [
            {
                breakpoint: 1100,
                settings: {
                  slidesToShow: 3,
                }
            },
            {
                breakpoint: 990,
                settings: {
                  slidesToShow: 2,
                  centerMode: false,
                }
            },
            {
                breakpoint: 576,
                settings: {
                  slidesToShow: 1,
                  centerMode: false,
                }
            }
        ]
    });
    $('.gallery_slider_list.lazy').slick({
        dots: false,
        arrows: true,
        infinite: true,
        speed: 1000,
        lazyLoad: 'progressive',
        cssEase: 'ease',
        slidesToShow: 3,
        slidesToScroll: 1,
        // autoplay: true,
        // autoplaySpeed: 2000,
        nextArrow:
        '<div class="slick-custom-arrow slick-custom-arrow-left orangeglow"><svg width="60" height="60" viewBox="0 0 60 60" fill="none" xmlns="http://www.w3.org/2000/svg"><circle cx="30" cy="30" r="30" fill="#fff"/><mask id="mask0_442_2" style="mask-type:alpha" maskUnits="userSpaceOnUse" x="18" y="18" width="24" height="24"><path d="M18 18H42V42H18V18Z" fill="white"/></mask><g mask="url(#mask0_442_2)"><path d="M27 36L33 30L27 24" stroke="#000" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/></g></svg></div>',
        prevArrow:
        '<div class="slick-custom-arrow slick-custom-arrow-right orangeglow"><svg width="60" height="60" viewBox="0 0 60 60" fill="none" xmlns="http://www.w3.org/2000/svg"><circle cx="30" cy="30" r="30" fill="#fff"/><mask id="mask0_1_10554" style="mask-type:alpha" maskUnits="userSpaceOnUse" x="18" y="18" width="24" height="24"><path d="M18 18H42V42H18V18Z" fill="white"/></mask><g mask="url(#mask0_1_10554)"><path d="M33 24L27 30L33 36" stroke="#000" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/></g></svg></div>',
        responsive: [
            {
                breakpoint: 1100,
                settings: {
                  slidesToShow: 3,
                }
            },
            {
                breakpoint: 990,
                settings: {
                  slidesToShow: 2,
                }
            },
            {
                breakpoint: 435,
                settings: {
                  slidesToShow: 1,
                }
            }
        ]
    });
});

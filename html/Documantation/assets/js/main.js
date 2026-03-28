(function ($) {
    "use strict";

    $(window).on("load", function () {
        $("#gx-overlay").fadeOut("slow");
    });
    $(document).ready(function () {

        $("body").attr("data-gx-mode", "light");

        /*========== Sidebar ===========*/
        // mobileAndTabletCheck       
        window.mobileAndTabletCheck = function () {
            let check = false;
            (function (a) { if (/(android|bb\d+|meego).+mobile|avantgo|bada\/|blackberry|blazer|compal|elaine|fennec|hiptop|iemobile|ip(hone|od)|iris|kindle|lge |maemo|midp|mmp|mobile.+firefox|netfront|opera m(ob|in)i|palm( os)?|phone|p(ixi|re)\/|plucker|pocket|psp|series(4|6)0|symbian|treo|up\.(browser|link)|vodafone|wap|windows ce|xda|xiino|android|ipad|playbook|silk/i.test(a) || /1207|6310|6590|3gso|4thp|50[1-6]i|770s|802s|a wa|abac|ac(er|oo|s\-)|ai(ko|rn)|al(av|ca|co)|amoi|an(ex|ny|yw)|aptu|ar(ch|go)|as(te|us)|attw|au(di|\-m|r |s )|avan|be(ck|ll|nq)|bi(lb|rd)|bl(ac|az)|br(e|v)w|bumb|bw\-(n|u)|c55\/|capi|ccwa|cdm\-|cell|chtm|cldc|cmd\-|co(mp|nd)|craw|da(it|ll|ng)|dbte|dc\-s|devi|dica|dmob|do(c|p)o|ds(12|\-d)|el(49|ai)|em(l2|ul)|er(ic|k0)|esl8|ez([4-7]0|os|wa|ze)|fetc|fly(\-|_)|g1 u|g560|gene|gf\-5|g\-mo|go(\.w|od)|gr(ad|un)|haie|hcit|hd\-(m|p|t)|hei\-|hi(pt|ta)|hp( i|ip)|hs\-c|ht(c(\-| |_|a|g|p|s|t)|tp)|hu(aw|tc)|i\-(20|go|ma)|i230|iac( |\-|\/)|ibro|idea|ig01|ikom|im1k|inno|ipaq|iris|ja(t|v)a|jbro|jemu|jigs|kddi|keji|kgt( |\/)|klon|kpt |kwc\-|kyo(c|k)|le(no|xi)|lg( g|\/(k|l|u)|50|54|\-[a-w])|libw|lynx|m1\-w|m3ga|m50\/|ma(te|ui|xo)|mc(01|21|ca)|m\-cr|me(rc|ri)|mi(o8|oa|ts)|mmef|mo(01|02|bi|de|do|t(\-| |o|v)|zz)|mt(50|p1|v )|mwbp|mywa|n10[0-2]|n20[2-3]|n30(0|2)|n50(0|2|5)|n7(0(0|1)|10)|ne((c|m)\-|on|tf|wf|wg|wt)|nok(6|i)|nzph|o2im|op(ti|wv)|oran|owg1|p800|pan(a|d|t)|pdxg|pg(13|\-([1-8]|c))|phil|pire|pl(ay|uc)|pn\-2|po(ck|rt|se)|prox|psio|pt\-g|qa\-a|qc(07|12|21|32|60|\-[2-7]|i\-)|qtek|r380|r600|raks|rim9|ro(ve|zo)|s55\/|sa(ge|ma|mm|ms|ny|va)|sc(01|h\-|oo|p\-)|sdk\/|se(c(\-|0|1)|47|mc|nd|ri)|sgh\-|shar|sie(\-|m)|sk\-0|sl(45|id)|sm(al|ar|b3|it|t5)|so(ft|ny)|sp(01|h\-|v\-|v )|sy(01|mb)|t2(18|50)|t6(00|10|18)|ta(gt|lk)|tcl\-|tdg\-|tel(i|m)|tim\-|t\-mo|to(pl|sh)|ts(70|m\-|m3|m5)|tx\-9|up(\.b|g1|si)|utst|v400|v750|veri|vi(rg|te)|vk(40|5[0-3]|\-v)|vm40|voda|vulc|vx(52|53|60|61|70|80|81|83|85|98)|w3c(\-| )|webc|whit|wi(g |nc|nw)|wmlb|wonu|x700|yas\-|your|zeto|zte\-/i.test(a.substr(0, 4))) check = true; })(navigator.userAgent || navigator.vendor || window.opera);
            return check;
        };

        function winSizeWidth() {
            var width = $(window).width();
            width = isMobTab ? width : width + 17;
            return width;
        }

        var currentActiveTab = localStorage.getItem('currentActiveTab') ?? null;
        var currentActiveSubTab = localStorage.getItem('currentActiveSubTab') ?? null;
        var currentSubLink = localStorage.getItem('currentSubLink') ?? null;

        var isMobTab = mobileAndTabletCheck();
        var screenSize = winSizeWidth();

        var sSize = {
            min: 576,
            max: 992,
        }

        function hideShowSidebar(el, activeEl, className, type) {
            if (sSize.max > screenSize) {
                if (sSize.min >= screenSize) {
                    $(el).show();
                    $(activeEl).addClass(className);
                } else {
                    if ($(".wrapper").hasClass("sb-default")) {
                        if (type == "click") {
                            $(el).show();
                            $(activeEl).addClass(className);
                        } else {
                            $(el).hide();
                            $(activeEl).removeClass(className);
                        }
                    }

                    if ($(".wrapper").hasClass("sb-collapse")) {
                        if (type == "resize" || type == "click") {
                            $(el).hide();
                            $(activeEl).removeClass(className);
                        } else {
                            $(el).show();
                            $(activeEl).addClass(className);
                        }
                    }

                }

            } else {
                if ($(".wrapper").hasClass("sb-default")) {
                    $(el).show();
                    $(activeEl).addClass(className);
                }

                if ($(".wrapper").hasClass("sb-collapse")) {
                    if (type == "mouseenter") {
                        $(el).show();
                        $(activeEl).addClass(className);
                    } else {
                        $(el).hide();
                        $(activeEl).removeClass(className);
                    }

                }
            }
        }

        function sidebarActiveTabs(type = '') {
            screenSize = winSizeWidth();
            $(".gx-sb-drop").hide();
            $(".gx-sb-subdrop.condense").hide();

            if (currentActiveTab != '') {
                var currentActiveEle = $(`span.condense:contains('${currentActiveTab}')`).filter(function () {
                    return $(this).text() === currentActiveTab;
                });
                var activeEl = $(currentActiveEle).parents('.gx-sb-item');
                var dropEl = $(currentActiveEle).parents('.gx-sb-item').find('.gx-sb-drop');
                hideShowSidebar(dropEl, activeEl, 'load-active', type);
            }

            if (currentActiveSubTab != '') {
                var currentSubTabActiveEle = $(`span.condense:contains('${currentActiveSubTab}')`).filter(function () {
                    return $(this).text() === currentActiveSubTab;
                });
                $(currentSubTabActiveEle).parents('.sb-subdrop-item').find('.gx-sb-subdrop.condense').show();
                var activeEl = $(currentSubTabActiveEle).parents('.sb-subdrop-item');
                var dropEl = $(currentSubTabActiveEle).parents('.sb-subdrop-item').find('.gx-sb-subdrop');
                hideShowSidebar(dropEl, activeEl, 'load-sub-active', type);
            }

            if (currentSubLink != '') {
                var currentSubActiveEle = $(`a.gx-page-link:contains('${currentSubLink}')`).filter(function () {
                    return $(this).text() === currentSubLink;
                });
                $(currentSubActiveEle).addClass('active-link');
                var activeEl = $(currentSubActiveEle).parents('.gx-sb-item');
                var dropEl = $(currentSubActiveEle).parents('.gx-sb-drop');
                hideShowSidebar(dropEl, activeEl, 'load-active', type);
            }
        }

        var newURL = window.location.pathname;
        var newURL = newURL.split("/").pop();
        $(".gx-sb-drop").hide();

        if (sSize.max > screenSize) {
            if (sSize.min >= screenSize) {

                $(".condense:not(.gx-sb-drop)").hide();
            } else {
                $(".wrapper").toggleClass("sb-collapse sb-default");

                $(".condense:not(.gx-sb-drop)").hide();
            }
        }
        if ($(".wrapper").hasClass("sb-default")) {
            $('.gx-sb-drop').hide();
            $("a.gx-page-link").filter(`[href='${newURL}']`).parent().parent().show();
            $("a.gx-page-link").filter(`[href='${newURL}']`).parent().parent().parent().addClass('load-active');
            $("a.gx-page-link").filter(`[href='${newURL}']`).addClass('active-link');

            var currentActiveLnk = $("a.gx-page-link").filter(`[href='${newURL}']`);

            if (currentActiveLnk.length > 0) {
                setgxPagelink($(currentActiveLnk));
            }

            var lastURL = localStorage.getItem('URL');

            sidebarActiveTabs();

            localStorage.setItem('URL', newURL);
        }

        $(".gx-drop-toggle").on("click", function (e) {
            var senderElement = e.target;

            if ($(senderElement).hasClass('gx-sub-drop-toggle')) return;
            if ($(senderElement).hasClass('gx-page-link')) return;
            if ($(senderElement).hasClass('condense') && $(senderElement).parents('.gx-sub-drop-toggle').length > 0) return;

            var parent = $(this).parents('.sb-drop-item');
            currentActiveTab = $(parent).find('.gx-drop-toggle span.condense').text();

            if ($(parent).hasClass('load-active')) {
                $(parent).find(".gx-sb-drop").slideUp();
                $(parent).removeClass('load-active');
                currentSubLink = currentActiveSubTab = currentActiveTab = '';
                localStorage.setItem('currentActiveTab', '');
                localStorage.setItem('currentActiveSubTab', '');
                localStorage.setItem('currentSubLink', '');
            } else {
                $('.load-active').removeClass('load-active');
                $(".gx-sb-drop").slideUp();
                $(parent).addClass('load-active');
                $(parent).find(".gx-sb-drop").slideDown();
                localStorage.setItem('currentActiveTab', currentActiveTab);
                currentSubLink = '';
                localStorage.setItem('currentSubLink', '');
            }
        });

        $(".gx-sub-drop-toggle").on("click", function (e) {
            var senderElement = e.target;

            var parent = $(this).parents('.sb-subdrop-item');
            currentActiveSubTab = $(parent).find('.gx-sub-drop-toggle span.condense').text();

            if ($(parent).hasClass('load-sub-active')) {
                $(parent).find(".gx-sb-subdrop").slideUp();
                $(parent).removeClass('load-sub-active');
                currentActiveSubTab = currentSubLink = '';
                localStorage.setItem('currentActiveSubTab', '');
                localStorage.setItem('currentSubLink', '');
            } else {
                $('.load-sub-active').removeClass('load-sub-active');
                $(".gx-sb-subdrop").hide();
                $(parent).addClass('load-sub-active');
                $(parent).find(".gx-sb-subdrop").slideDown();
                localStorage.setItem('currentActiveSubTab', currentActiveSubTab);
            }
        });

        function setgxPagelink(_this) {
            $('.active-link').removeClass('active-link');

            currentSubLink = $(_this).text();

            if (currentSubLink != '') {
                localStorage.setItem('currentSubLink', currentSubLink);
            }

            $(_this).addClass('active-link');

            // sb-drop-item
            const mainParentHas = $(_this).parents('.sb-drop-item');

            if (mainParentHas) {
                currentActiveTab = $(mainParentHas).find('.gx-drop-toggle span.condense').text();

                localStorage.setItem('currentActiveTab', currentActiveTab);
            }

            // Sub Parent Item
            const subParentHas = $(_this).parents('.sb-subdrop-item');
            if (subParentHas) {
                currentActiveSubTab = $(subParentHas).find('.gx-sub-drop-toggle span.condense').text();

                localStorage.setItem('currentActiveSubTab', currentActiveSubTab);
            }
        }

        $(".gx-page-link").on("click", function (e) {
            setgxPagelink($(this));
        });

        $(window).resize(function (e) {
            screenSize = winSizeWidth();
            if (sSize.max >= screenSize) {
                if ($(".wrapper").hasClass("sb-default")) {
                    $(".gx-sidebar-overlay").fadeOut();

                    if (sSize.min <= screenSize) {
                        if ($(".gx-toggle-sidebar").hasClass('active')) {
                            $(".gx-toggle-sidebar").removeClass('active');
                        }
                    } else {
                        if (!$(".gx-toggle-sidebar").hasClass('active')) {
                            $(".gx-toggle-sidebar").addClass('active');
                        }
                    }

                    $(".wrapper").removeClass("sb-default").addClass('sb-collapse');

                    $(".condense:not(.gx-sb-drop)").hide();
                    sidebarActiveTabs(e.type);
                }
            }
            if (sSize.max < screenSize || sSize.min >= screenSize) {

                if ($(".wrapper").hasClass("sb-collapse")) {
                    $(".gx-sidebar-overlay").fadeOut();
                    if (sSize.min >= screenSize) {
                        if ($(".gx-toggle-sidebar").hasClass('active')) {
                            $(".gx-toggle-sidebar").removeClass('active');
                        }
                    } else {
                        if (!$(".gx-toggle-sidebar").hasClass('active')) {
                            $(".gx-toggle-sidebar").addClass('active');
                        }
                    }

                    $(".wrapper").removeClass('sb-collapse').addClass("sb-default");
                    $(".condense:not(.gx-sb-drop)").show();
                    sidebarActiveTabs(e.type);
                }
            }


        });

        $(".gx-sidebar-overlay").on('click', function (e) {
            $(".gx-sidebar-overlay").fadeOut();

            $(".wrapper").toggleClass("sb-collapse sb-default");

            $(".condense:not(.gx-sb-drop)").hide();

            $(".gx-toggle-sidebar").removeClass('active');

            sidebarActiveTabs(e.type);
        });

        // On click Toggle sidebar collapse
        $(".gx-toggle-sidebar").on("click", function (e) {
            screenSize = winSizeWidth();
            if (sSize.max > screenSize) {
                $(".gx-sidebar-overlay").fadeIn();
            }
            $(".wrapper").toggleClass("sb-collapse sb-default");
            $(this).toggleClass("active");
            if ($(".wrapper").hasClass("sb-default")) {
                $(".condense").show();
                $(".gx-sb-drop").hide();

                sidebarActiveTabs(e.type);

            } else {
                if (sSize.max < screenSize) {
                    $(".condense:not(.gx-sb-drop)").hide();
                } else {
                    $(".condense:not(.gx-sb-drop)").show();
                    $(".condense.gx-sb-drop").hide();
                }
                sidebarActiveTabs(e.type);
            }

        });
        $('.gx-sidebar, .sb-collapse').on("mouseenter", function (e) {
            screenSize = winSizeWidth();
            if (sSize.max < screenSize) {
                if (!$(".wrapper").hasClass("sb-default")) {
                    $(".condense:not(.gx-sb-drop)").show();
                }
                sidebarActiveTabs(e.type);
            }
        });

        $('.gx-sidebar').on("mouseleave", function (e) {
            screenSize = winSizeWidth();
            if (sSize.max < screenSize) {
                if (!$(".wrapper").hasClass("sb-default")) {
                    $(".condense:not(.gx-sb-drop)").hide();

                }
                sidebarActiveTabs(e.type);
            }
        });

        /*========== Header tools ===========*/
        $(".gx-screen.full").on("click", function () {
            $(this).css("display", "none");
            $(".gx-screen.reset").css("display", "flex");

            // current working methods
            if (document.documentElement.requestFullscreen) {
                document.documentElement.requestFullscreen();
            } else if (document.documentElement.msRequestFullscreen) {
                document.documentElement.msRequestFullscreen();
            } else if (document.documentElement.mozRequestFullScreen) {
                document.documentElement.mozRequestFullScreen();
            } else if (document.documentElement.webkitRequestFullscreen) {
                document.documentElement.webkitRequestFullscreen(
                    Element.ALLOW_KEYBOARD_INPUT
                );
            }
        });
        $(".gx-screen.reset").on("click", function () {
            $(this).css("display", "none");
            $(".gx-screen.full").css("display", "flex");
            if (document.exitFullscreen) {
                document.exitFullscreen();
            } else if (document.msExitFullscreen) {
                document.msExitFullscreen();
            } else if (document.mozCancelFullScreen) {
                document.mozCancelFullScreen();
            } else if (document.webkitExitFullscreen) {
                document.webkitExitFullscreen();
            }
        });
        var $addLink = $('<link>', {
            rel: 'stylesheet',
            href: 'assets/css/dark.css',
            id: 'dark'
        });

        $(".gx-mode.dark").on("click", function () {
            $(this).css("display", "none");
            $(".gx-mode.light").css("display", "flex");

            $("body").attr("data-gx-mode", "dark");
            $("#mainCss").after($addLink);
            var headerModes = $(".gx-tools-item.header").attr("data-header-mode");
            if (headerModes == "light") {
                $(".gx-tools-item.header[data-header-mode='dark']").addClass("active");
                $(".gx-tools-item.header[data-header-mode='light']").removeClass("active");
                $(".gx-header").attr("data-header-mode-tool", "dark");
            }
            $(".gx-tools-item.mode[data-gx-mode-tool='light']").removeClass("active");
            $(".gx-tools-item.mode[data-gx-mode-tool='dark']").addClass("active");
        });
        $(".gx-mode.light").on("click", function () {
            $(this).css("display", "none");
            $(".gx-mode.dark").css("display", "flex");
            $(".gx-header").attr("data-header-mode-tool", "light");

            $("body").attr("data-gx-mode", "light");
            $("#dark").remove();
            var headerModes = $(".gx-tools-item.header").attr("data-header-mode");
            if (headerModes == "light") {
                $(".gx-tools-item.header[data-header-mode='light']").addClass("active");
                $(".gx-tools-item.header[data-header-mode='dark']").removeClass("active");
                $(".gx-header").attr("data-header-mode-tool", "light");
            }
            $(".gx-tools-item.mode[data-gx-mode-tool='dark']").removeClass("active");
            $(".gx-tools-item.mode[data-gx-mode-tool='light']").addClass("active");
        });

        $(".gx-notify").on("click", function () {
            $(".gx-notify-bar").addClass("gx-notify-bar-open");
            $(".gx-notify-bar-overlay").fadeIn();
        });
        $(".gx-notify-bar-overlay, .close-notify").on("click", function () {
            $(".gx-notify-bar").removeClass("gx-notify-bar-open");
            $(".gx-notify-bar-overlay").fadeOut();
        });

        $(".open-search").on("click", function () {
            $(".gx-search").fadeIn();
        });

        /*========== Search Remix icon page ===========*/
        $('[data-search-icon]').on('keyup', function () {
            var searchVal = $(this).val().toLowerCase();
            var filterItems = $('[data-search-item]');
            var filterItemsText = $('[data-search-item]').text().toLowerCase();
            var a = $('[data-search-item]:contains(' + searchVal + ')');
            if (searchVal != '') {
                filterItems.closest(".remix-unicode-icon").addClass('hide');
                $('[data-search-item]:contains(' + searchVal + ')').closest(".remix-unicode-icon").removeClass('hide');
            } else {
                filterItems.closest(".remix-unicode-icon").removeClass('hide');
            }
        });

        /*========== Search Material icon page ===========*/
        $('[data-search-material]').on('keyup', function () {
            var searchVal = $(this).val().toLowerCase();
            var filterItems = $('.material-search-item');
            var filterItemsText = $('.material-search-item').text().toLowerCase();
            var a = $('.material-search-item:contains(' + searchVal + ')');
            if (searchVal != '') {
                filterItems.closest(".material-icon-item").addClass('hide');
                $('.material-search-item:contains(' + searchVal + ')').closest(".material-icon-item").removeClass('hide');
            } else {
                filterItems.closest(".material-icon-item").removeClass('hide');
            }
        });
    });

    /*========== On ckick card zoom (full screen) ===========*/
    $(".gx-full-card").on("click", function () {
        $(this).hide();
        $(this).parent(".header-tools").append('<a href="javascript:void(0)" class="m-l-10 gx-full-card-close"><i class="ri-close-fill"></i></a>');
        $(this).closest(".gx-card").parent().toggleClass("gx-full-screen");
        $(this).closest(".gx-card").parent().parent().append('<div class="gx-card-overlay"></div>');
    });
    $("body").on("click", ".gx-card-overlay, .gx-full-card-close", function () {
        $(".gx-card").find(".gx-full-card-close").remove();
        $(".gx-card").find(".gx-full-card").show();
        $(".gx-card").parent().removeClass("gx-full-screen");
        $(".gx-card-overlay").remove();
    });

    /*========== Tools Sidebar ===========*/
    $('.gx-tools-sidebar-toggle').on("click", function () {
        $('.gx-tools-sidebar').addClass("open-tools");
        $('.gx-tools-sidebar-overlay').fadeIn();
        $('.gx-tools-sidebar-toggle').hide();

    });
    $('.gx-tools-sidebar-overlay, .close-tools').on("click", function () {
        $('.gx-tools-sidebar').removeClass("open-tools");
        $('.gx-tools-sidebar-overlay').fadeOut();
        $('.gx-tools-sidebar-toggle').fadeIn();

    });

    /*========== Topic Sidebar ===========*/
    $('.btn-topic').on("click", function () {
        $('.gx-topic').addClass("gx-open-topic");
        $('.gx-side-overlay').show();
    });
    $('.gx-side-overlay').on("click", function () {
        $('.gx-topic').removeClass("gx-open-topic");
        $('.gx-side-overlay').hide();
    });

    /*========== Structure dowpdown ===========*/
    $('.gx-hide').slideUp();
    $('.gx-struct-drop').on("click", function () {
        $(this).parent("li").children("ul").slideToggle();
        $(this).parent().parent("ul").toggleClass("active");
    });

    /*========== Scroll Section to section ===========*/
    var sections = $('.gx-page-block')
        , nav = $('nav')
        , nav_height = nav.outerHeight();

    $(window).on('scroll', function () {
        var cur_position = $(this).scrollTop();

        sections.each(function () {
            var top = $(this).offset().top - nav_height,
                bottom = top + $(this).outerHeight();

            if (cur_position >= top && cur_position <= bottom) {
                nav.find('a').removeClass('active');
                sections.removeClass('active');

                $(this).addClass('active');
                nav.find('a[href="#' + $(this).attr('id') + '"]').addClass('active');
            }
        });
    });

    nav.find('a').on('click', function () {
        var $el = $(this)
            , id = $el.attr('href');

        $('html, body').animate({
            scrollTop: $(id).offset().top - nav_height
        }, 50);

        return false;
    });

    /*========== Tools Sidebar ===========*/
    // Mode 
    var $link = $('<link>', {
        rel: 'stylesheet',
        href: 'assets/css/dark.css',
        id: 'dark'
    });
    $('.gx-tools-item.mode').on("click", function () {
        var modes = $(this).attr("data-gx-mode-tool");
        if (modes == "light") {
            $("body").attr("data-gx-mode", "light");
            $("#dark").remove();
            var headerModes = $(".gx-tools-item.header").attr("data-header-mode");
            if (headerModes == "light") {
                $(".gx-tools-item.header[data-header-mode='light']").addClass("active");
                $(".gx-tools-item.header[data-header-mode='dark']").removeClass("active");
                $(".gx-header").attr("data-header-mode-tool", "light");
            }
            $(".gx-mode.light").css("display", "none");
            $(".gx-mode.dark").css("display", "flex");

        } else if (modes == "dark") {
            $("body").attr("data-gx-mode", "dark");
            $("#mainCss").after($link);
            var headerModes = $(".gx-tools-item.header").attr("data-header-mode");
            if (headerModes == "light") {
                $(".gx-tools-item.header[data-header-mode='dark']").addClass("active");
                $(".gx-tools-item.header[data-header-mode='light']").removeClass("active");
                $(".gx-header").attr("data-header-mode-tool", "dark");
            }
            $(".gx-mode.dark").css("display", "none");
            $(".gx-mode.light").css("display", "flex");
        }

        $(this).parents(".gx-tools-info").find('.gx-tools-item.mode').removeClass("active")
        $(this).addClass("active");
    });
    // Sidebar 
    $('.gx-tools-item.sidebar').on("click", function () {
        var sidebarModes = $(this).attr("data-sidebar-mode-tool");
        if (sidebarModes == "light") {
            $('.gx-sidebar').attr('data-mode', 'light');
        } else if (sidebarModes == "dark") {
            $('.gx-sidebar').attr('data-mode', 'dark');
        } else if (sidebarModes == "bg-1") {
            $('.gx-sidebar').attr('data-mode', 'bg-1');
        } else if (sidebarModes == "bg-2") {
            $('.gx-sidebar').attr('data-mode', 'bg-2');
        } else if (sidebarModes == "bg-3") {
            $('.gx-sidebar').attr('data-mode', 'bg-3');
        } else if (sidebarModes == "bg-4") {
            $('.gx-sidebar').attr('data-mode', 'bg-4');
        }
        $(this).parents(".gx-tools-info").find('.gx-tools-item.sidebar').removeClass("active")
        $(this).addClass("active");
    });
    // Backgrounds 
    $('.gx-tools-item.bg').on("click", function () {
        var bgModes = $(this).attr("data-bg-mode");
        if (bgModes == "default") {
            $('#mainBg').remove();
        } else if (bgModes == "bg-1") {
            $('#mainBg').remove();
            $("#mainCss").after("<link id='mainBg' href='assets/css/bg-1.css' rel='stylesheet'>");
        } else if (bgModes == "bg-2") {
            $('#mainBg').remove();
            $("#mainCss").after("<link id='mainBg' href='assets/css/bg-2.css' rel='stylesheet'>");
        } else if (bgModes == "bg-3") {
            $('#mainBg').remove();
            $("#mainCss").after("<link id='mainBg' href='assets/css/bg-3.css' rel='stylesheet'>");
        } else if (bgModes == "bg-4") {
            $('#mainBg').remove();
            $("#mainCss").after("<link id='mainBg' href='assets/css/bg-4.css' rel='stylesheet'>");
        } else if (bgModes == "bg-5") {
            $('#mainBg').remove();
            $("#mainCss").after("<link id='mainBg' href='assets/css/bg-5.css' rel='stylesheet'>");
        }
        $(this).parents(".gx-tools-info").find('.gx-tools-item.bg').removeClass("active")
        $(this).addClass("active");
    });
    // Box design
    $('.gx-tools-item.box').on("click", function () {
        var boxModes = $(this).attr("data-box-mode-tool");
        $("#box_design").remove();
        if (boxModes == "default") {
            $("#box_design").remove();
        } else if (boxModes == "box-1") {
            $("#mainCss").after('<link id="box_design" href="assets/css/box-1.css" rel="stylesheet">');
        } else if (boxModes == "box-2") {
            $("#mainCss").after('<link id="box_design" href="assets/css/box-2.css" rel="stylesheet">');
        } else if (boxModes == "box-3") {
            $("#mainCss").after('<link id="box_design" href="assets/css/box-3.css" rel="stylesheet">');
        }
        $(this).parents(".gx-tools-info").find('.gx-tools-item.box').removeClass("active")
        $(this).addClass("active");
    });
})(jQuery);
/* ------------------------------------- Cursor Js ------------------------------------- */
function initTrion() {
    //   loader ------------------
    "use strict";
    firstLoad();
    function firstLoad() {
      setTimeout(function () {
        $(".main-loader-wrap .loader-spin").addClass("novisspin");
      }, 1500);
      setTimeout(function () {
        $(".main-loader-wrap").fadeOut(500);
      }, 2200);
      var chdpt = $(".content-holder").data("pagetitle");
      $(".breadcrumb-wrap span").text(chdpt);
    }
  
    // CURSOR
  
    $("a.custom_cursor_orangeglow").on({
      mouseenter: function () {
        $(".megic-cursor-item").addClass("custom_cursor_orangeglow");
      },
      mouseleave: function () {
        $(".megic-cursor-item").removeClass("custom_cursor_orangeglow");
      },
    });
    $("a.custom_cursor_whiteglow").on({
      mouseenter: function () {
        $(".megic-cursor-item").addClass("custom_cursor_whiteglow");
      },
      mouseleave: function () {
        $(".megic-cursor-item").removeClass("custom_cursor_whiteglow");
      },
    });
    $("a.custom_cursor_whiteborder").on({
      mouseenter: function () {
        $(".megic-cursor-item").addClass("custom_cursor_whiteborder");
      },
      mouseleave: function () {
        $(".megic-cursor-item").removeClass("custom_cursor_whiteborder");
      },
    });
    $("a.custom_cursor_blackborder").on({
      mouseenter: function () {
        $(".megic-cursor-item").addClass("custom_cursor_blackborder");
      },
      mouseleave: function () {
        $(".megic-cursor-item").removeClass("custom_cursor_blackborder");
      },
    });
    $("a.triners_name").on({
      mouseenter: function () {
        $(".megic-cursor-item").addClass("triners_name");
      },
      mouseleave: function () {
        $(".megic-cursor-item").removeClass("triners_name");
      },
    });
    $("a.triners_icons").on({
      mouseenter: function () {
        $(".megic-cursor-item").addClass("triners_icons");
      },
      mouseleave: function () {
        $(".megic-cursor-item").removeClass("triners_icons");
      },
    });
    $("a.contact_hover_btn").on({
      mouseenter: function () {
        $(".megic-cursor-item").addClass("contact_hover_btn");
      },
      mouseleave: function () {
        $(".megic-cursor-item").removeClass("contact_hover_btn");
      },
    });
    $("a.our_traning_btn_box").on({
      mouseenter: function () {
        $(".megic-cursor-item").addClass("our_traning_btn_box");
      },
      mouseleave: function () {
        $(".megic-cursor-item").removeClass("our_traning_btn_box");
      },
    });
    $("a.tabimg_glow").on({
      mouseenter: function () {
        $(".megic-cursor-item").addClass("tabimg_glow");
      },
      mouseleave: function () {
        $(".megic-cursor-item").removeClass("tabimg_glow");
      },
    });
    $("button.coming_soon_glow").on({
      mouseenter: function () {
        $(".megic-cursor-item").addClass("coming_soon_glow");
      },
      mouseleave: function () {
        $(".megic-cursor-item").removeClass("coming_soon_glow");
      },
    });
    $("div.orangeglow").on({
      mouseenter: function () {
        $(".megic-cursor-item").addClass("orangeglow");
      },
      mouseleave: function () {
        $(".megic-cursor-item").removeClass("orangeglow");
      },
    });
    $("div.whiteglow").on({
      mouseenter: function () {
        $(".megic-cursor-item").addClass("whiteglow");
      },
      mouseleave: function () {
        $(".megic-cursor-item").removeClass("whiteglow");
      },
    });
    $("div.custom_cursor_blackborder").on({
      mouseenter: function () {
        $(".megic-cursor-item").addClass("custom_cursor_blackborder");
      },
      mouseleave: function () {
        $(".megic-cursor-item").removeClass("custom_cursor_blackborder");
      },
    });
    $("div.tabimg_glow").on({
      mouseenter: function () {
        $(".megic-cursor-item").addClass("tabimg_glow");
      },
      mouseleave: function () {
        $(".megic-cursor-item").removeClass("tabimg_glow");
      },
    });
    $("span.orangeglow_btn").on({
      mouseenter: function () {
        $(".megic-cursor-item").addClass("orangeglow_btn");
      },
      mouseleave: function () {
        $(".megic-cursor-item").removeClass("orangeglow_btn");
      },
    });
    $("span.whiteglow_btn").on({
      mouseenter: function () {
        $(".megic-cursor-item").addClass("whiteglow_btn");
      },
      mouseleave: function () {
        $(".megic-cursor-item").removeClass("whiteglow_btn");
      },
    });
  
    $("  #portfolio_horizontal_container").on({
      mouseenter: function () {
        $(".element-item").addClass("slider_hover");
      },
      mouseleave: function () {
        $(".element-item").removeClass("slider_hover");
      },
    });
  
    // CURSOR
  }
  
  //   load animation------------------
  $.fn.duplicate = function (a, b) {
    var c = [];
    for (var d = 0; d < a; d++) $.merge(c, this.clone(b).get());
    return this.pushStack(c);
  };
  
  var a = {
    Android: function () {
      return navigator.userAgent.match(/Android/i);
    },
    BlackBerry: function () {
      return navigator.userAgent.match(/BlackBerry/i);
    },
    iOS: function () {
      return navigator.userAgent.match(/iPhone|iPad|iPod/i);
    },
    Opera: function () {
      return navigator.userAgent.match(/Opera Mini/i);
    },
    Windows: function () {
      return navigator.userAgent.match(/IEMobile/i);
    },
    any: function () {
      return a.Android() || a.BlackBerry() || a.iOS() || a.Opera() || a.Windows();
    },
  };
  trueMobile = a.any();
  if (null == trueMobile) {
    $(function () {
      $.coretemp({
        reloadbox: "#wrapper",
        outDuration: 1200,
        inDuration: 100,
      });
      readyFunctions();
      $(document).on({
        ksctbCallback: function () {
          readyFunctions();
        },
      });
    });
    function readyFunctions() {
      initTrion();
    }
  }
  if (trueMobile) {
    $(document).ready(function () {
      initTrion();
    });
  
    $("html, body").animate(
      {
        scrollTop: 0,
      },
      1
    );
  }
  $("head").append(
    '<meta name="viewport" content="width=device-width, initial-scale=1.0, maximum-scale=1.0, user-scalable=0">'
  );
  document.addEventListener("gesturestart", function (e) {
    e.preventDefault();
  });
  
  if ($(".megic-cursor-item").length > 0) {
    var mouse = {
      x: 0,
      y: 0,
    };
    var pos = {
      x: 0,
      y: 0,
    };
    var ratio = 0.15;
    var active = false;
    var ball = document.querySelector(".megic-cursor-item");
    TweenLite.set(ball, {
      xPercent: -50,
      yPercent: -50,
    });
    document.addEventListener("mousemove", mouseMove);
    function mouseMove(e) {
      var scrollTop = window.pageYOffset || document.documentElement.scrollTop;
      mouse.x = e.pageX;
      mouse.y = e.pageY - scrollTop;
    }
    TweenMax.ticker.addEventListener("tick", updatePosition);
    function updatePosition() {
      if (!active) {
        pos.x += (mouse.x - pos.x) * ratio;
        pos.y += (mouse.y - pos.y) * ratio;
        TweenMax.set(ball, {
          x: pos.x,
          y: pos.y,
        });
      }
    }
  }
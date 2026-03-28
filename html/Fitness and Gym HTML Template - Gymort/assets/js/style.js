/* ------------------------------------- Preloader ------------------------------------- */
$(window).on("load", function () {
  setTimeout(function () {
    var preloader = document.querySelector(".preloader");
    preloader.style.opacity = "0";
    $(".preloader").fadeOut("slow");
  }, 500);
});

/*------------------------------------- Sticky Header -------------------------------------*/
$(window).scroll(function () {
  if ($(window).scrollTop() >= 20) {
    $("#header_sec").addClass("sticky");
  } else {
    $("#header_sec").removeClass("sticky");
  }
});

/* ------------------------------------- Header Dropdown-Menu ------------------------------------- */
$(document).ready(function () {
  $(".drop-down").click(function (e) {
    e.preventDefault();
    $(this).next("ul.drop_down_menu").slideToggle(300);
    $(this).next("ul.inner_dropdown_menu").slideToggle(300);
  });
});
/* ------------------------------------- Header Search ------------------------------------- */
$(document).ready(function () {
  $(".headersearch").click(function () {
    $(".search-section").toggleClass("open");
  });
});

const shineElements = document.querySelectorAll(".shine");

shineElements.forEach((shine) => {
  shine.addEventListener("mouseover", () => {
    shine.style.opacity = 1;
  });

  shine.addEventListener("mouseout", () => {
    shine.style.opacity = 0;
  });
});
/* ------------------------------------- Custome Animation ------------------------------------- */
function reveal() {
  var reveals = document.querySelectorAll(".reveal");

  for (var i = 0; i < reveals.length; i++) {
    var windowHeight = window.innerHeight;
    var elementTop = reveals[i].getBoundingClientRect().top;
    var elementVisible = 25;

    if (elementTop < windowHeight - elementVisible) {
      reveals[i].classList.add("active");
    } else {
      reveals[i].classList.remove("active");
    }
  }
}

window.addEventListener("scroll", reveal);

/* ------------------------------------- Progress-Image Animation ------------------------------------- */
document.addEventListener("DOMContentLoaded", function () {
  const progressImages = document.querySelectorAll(".progress_img");

  progressImages.forEach((hoverContainer) => {
    const progressImg = hoverContainer.querySelector(".progress_img_pera");

    hoverContainer.addEventListener("mousemove", (e) => {
      let rect = hoverContainer.getBoundingClientRect();
      let x = (e.clientX - rect.left - rect.width / 2) / 20;
      let y = (e.clientY - rect.top - rect.height / 2) / 20;
      progressImg.style.transition = "none";
      progressImg.style.transform = `rotateY(${x}deg) rotateX(${y}deg)`;
    });

    hoverContainer.addEventListener("mouseout", () => {
      progressImg.style.transform = `rotateY(0) rotateX(0)`;
      progressImg.style.transition = "all .2s ease";
    });
  });
});

/* -------------------- Video-Player -------------------- */
$(".js-overlay-start")
  .unbind("click")
  .bind("click", function (e) {
    e.preventDefault();
    var src = $(this).attr("data-url");
    $(".overlay-video").show();

    $(".overlay-video").addClass("o1");
    $("#player").attr("src", src);
  });
$(".our-video").click(function (event) {
  $(".overlay-video").show();
  let a = $(this).find("a").attr("data-url");
  $(".overlay-video").find("iframe").attr("src", a);
});
$(".close").click(function (event) {
  var PlayingVideoSrc = $("#player").attr("src").replace("&autoplay=1", "");
  $("#player").attr("src", PlayingVideoSrc);
  $(".overlay-video").removeClass("o1");
  setTimeout(function () {
    $(".overlay-video").hide();
  }, 600);
});

/* ------------------------------------- counter ------------------------------------- */
const stats = document.querySelectorAll(".counter");

stats.forEach((stat) => {
  const patt = /(\D+)?(\d+)(\D+)?(\d+)?(\D+)?/;
  const time = 1000;
  let result = [...patt.exec(stat.textContent)];
  let fresh = true;
  let ticks;

  result.shift();
  result = result.filter((res) => res != null);

  while (stat.firstChild) {
    stat.removeChild(stat.firstChild);
  }

  for (let res of result) {
    if (isNaN(res)) {
      stat.insertAdjacentHTML("beforeend", `<span>${res}</span>`);
    } else {
      for (let i = 0; i < res.length; i++) {
        stat.insertAdjacentHTML(
          "beforeend",
          `<span data-value="${res[i]}">
                <span></span>
                ${Array(parseInt(res[i]) + 1)
                  .join(0)
                  .split(0)
                  .map(
                    (_x, j) => `
                    <span>${j}</span>
                `
                  )
                  .join("")}
            </span>`
        );
      }
    }
  }

  ticks = [...stat.querySelectorAll("span[data-value]")];

  let activate = () => {
    let top = stat.getBoundingClientRect().top;
    let offset = window.innerHeight * 1;

    setTimeout(() => {
      fresh = false;
    }, time);

    if (top < offset) {
      setTimeout(
        () => {
          for (let tick of ticks) {
            let dist = parseInt(tick.getAttribute("data-value")) + 1;
            tick.style.transform = `translateY(-${dist * 100}%)`;
          }
        },
        fresh ? time : 0
      );
      window.removeEventListener("scroll", activate);
    }
  };
  window.addEventListener("scroll", activate);
  activate();
});
/* ------------------------------------- FAQ Accodian ------------------------------------- */
$(document).ready(function () {
  $(".faq_accodian_title").click(function () {
    $(this)
      .toggleClass("active")
      .next(".faq_accodian_tabs")
      .slideToggle()
      .parent()
      .siblings()
      .find(".faq_accodian_tabs")
      .slideUp()
      .prev()
      .removeClass("active");
  });
});

/* ------------------------------------- Banner-Accodian ------------------------------------- */
var div = $("li.yoga_training_item");
function toggleAccordion() {
  div.removeClass("active");
  $(this).addClass("active");
}
div.on("click", toggleAccordion);
/* ------------------------------------- Pricing Hover Animation ------------------------------------- */
document.addEventListener('DOMContentLoaded', (_event) => {
  const items = document.querySelectorAll('.pricing_plan_v2_box, .body_detail');

  items.forEach(item => {
      item.addEventListener('mouseenter', () => {
          items.forEach(i => i.classList.remove('active'));
          item.classList.add('active');
      });

      item.addEventListener('mouseleave', () => {
          item.classList.add('active');
      });
  });
});
/* ------------------------------------- Yoga Tab Animation ------------------------------------- */
document.addEventListener('DOMContentLoaded', () => {
  const items = document.querySelectorAll('.yoga_training_item');

  items.forEach(item => {
      item.addEventListener('click', () => {
          items.forEach(i => {
              i.classList.remove('active');
              const imageDiv = i.querySelector('.yoga_training_content_image');
              const infoDiv = i.querySelector('.yoga_training_content_image_info');
              if (imageDiv) imageDiv.classList.remove('animate__animated', 'animate__fadeInDown');
              if (infoDiv) infoDiv.classList.remove('animate__animated', 'animate__fadeInUp');
          });
          item.classList.add('active');

          const imageDiv = item.querySelector('.yoga_training_content_image');
          const infoDiv = item.querySelector('.yoga_training_content_image_info');
          if (imageDiv) imageDiv.classList.add('animate__animated', 'animate__fadeInDown');
          if (infoDiv) infoDiv.classList.add('animate__animated', 'animate__fadeInUp');
      });
  });
});

/* ------------------------------------- Images Tabs ------------------------------------- */
$(".my_program_content_tabs").hide().css("opacity", 0);
$(".my_program_content_tabs").eq(1).show().addClass("show").animate({ opacity: 1 }, 1000);
$(".program_tabs li").eq(1).addClass("active");

$(".program_tabs li a").on("click", function (event) {
  event.preventDefault();
  var target = $($(this).attr("href"));
  
  $(".my_program_content_tabs").removeClass("show").hide().css("opacity", 0);
  $(".program_tabs li").removeClass("active");
  $(this).parent().addClass("active");
  
  target.show().addClass("show").animate({ opacity: 1 }, 1000);
});

/* -------------------------------------  Project Tabs ------------------------------------- */
$(".our_project_content_tabs").hide().css("opacity", 0);
$(".our_project_content_tabs:first").show().animate({ opacity: 1 }, 1000);
$(".our_project_tabs li:first").addClass("active");
$(".our_project_tabs li a").on("click", function (event) {
  event.preventDefault();
  var target = $($(this).attr("href"));
  $(".our_project_content_tabs").hide().css("opacity", 0);
  $(".our_project_tabs li").removeClass("active");
  $(this).parent().addClass("active");
  target.show().animate({ opacity: 1 }, 1000);
});

/* -------------------------------------  Home Banner Tabs ------------------------------------- */

$(document).ready(function() {
  function activateTab(element) {
      var target = $(element.attr("href"));
      $(".home_banner_content_tabs").hide().removeClass("show").css("opacity", 0);
      $(".home_banner_tabs li").removeClass("active");
      element.parent().addClass("active");
      target.show().addClass("show").animate({ opacity: 1 }, 1000);
  }

  $(".home_banner_content_tabs").hide().css("opacity", 0);
  $(".home_banner_content_tabs:first").show().addClass("show").animate({ opacity: 1 }, 1000);
  $(".home_banner_tabs li:first").addClass("active");

  $(".home_banner_tabs li a").hover(
      function(event) {
          event.preventDefault();
          activateTab($(this));
      },
      function() {
      }
  );
});
/* ------------------------------------- Scroll To Top Button ------------------------------------- */
const scrollBtn = document.getElementById("scroll-top-btn");
const scrollPercentage = document.getElementById("scroll-percentage");

window.onscroll = function () {
  const scrolled =
    (window.scrollY /
      (document.documentElement.scrollHeight -
        document.documentElement.clientHeight)) *
    100;
  scrollPercentage.textContent = scrolled.toFixed(0) + "%";

  if (scrolled > 0) {
    document.body.classList.add("scrolled");
  } else {
    document.body.classList.remove("scrolled");
  }
};

scrollBtn.addEventListener("click", function () {
  window.scrollTo({ top: 0, behavior: "smooth" });
});

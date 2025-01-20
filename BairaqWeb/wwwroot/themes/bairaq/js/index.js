$(function () {
    if($(".block2-swiper").length > 0) {
        let blockSocietySwiper = null;
        if (window.innerWidth < 768) {
            blockSocietySwiper = new Swiper(".block2-swiper", {
                slidesPerView: 1,
                spaceBetween: 0,
            });
        } else {
            blockSocietySwiper = new Swiper(".block2-swiper", {
                slidesPerView: 4,
                spaceBetween: 32,
            });
        }
        $(".block2 svg.next").on("click", function () {
            blockSocietySwiper.slideNext();
        })
    
        $(".block2 svg.prev").on("click", function () {
            blockSocietySwiper.slidePrev();
        })
    }


    if($(".block5-swiper").length > 0) {

        let cultureSocietySwiper = null;
        if (window.innerWidth < 768) {
            cultureSocietySwiper = new Swiper(".block5-swiper", {
                slidesPerView: 1,
                spaceBetween: 0,
            });
        } else {
            cultureSocietySwiper = new Swiper(".block5-swiper", {
                slidesPerView: 4,
                spaceBetween: 32,
            });
        }

        $(".block5 svg.next").on("click", function () {
            cultureSocietySwiper.slideNext();
        })

        $(".block5 svg.prev").on("click", function () {
            cultureSocietySwiper.slidePrev();
        })
    }
    
    $(".youtube-thumbnail").on("click", function () {
        $(".youtube-iframe").attr("src", $(this).attr("data-src"));
        $(".youtube-video .left .title").text($(this).attr("data-title"));
        $(".youtube-video .left .date-time").text($(this).attr("data-date-time"));
    })
    
    $(".share a").on("click", async function (e) {
        e.preventDefault();
        const link = $(this).attr("href");
        await common.copy(link);
    })

    $('iframe[data-embed="social"]').on('load', function () {
        setTimeout(() => {
            const iframeBody = $(this).contents().find('body');
            if (!iframeBody) {
                return;
            }
            const height = iframeBody[0].scrollHeight;
            const width = iframeBody[0].scrollWidth;
            $(this).height(height);
            $(this).width(width);
            iframeBody.css('overflow', 'hidden');
            $(this).css('overflow', 'hidden');
        }, 100);
    });
    
    const resizeHandler = function() {
        if ($(window).width() <= 768) {
            $('header.header').addClass('fixed-top');
        } else {
            $('header.header').removeClass('fixed-top');
        }
    }

    resizeHandler();

    $(window).resize(resizeHandler);
})

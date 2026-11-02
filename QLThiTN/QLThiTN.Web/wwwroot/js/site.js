// Hệ thống ôn thi THPT Quốc gia - Site JS

$(function () {
    // Tự động ẩn alert sau 4 giây
    window.setTimeout(function () {
        $('.alert-dismissible').fadeOut('slow');
    }, 4000);

    // Danh dau menu dang mo (active) theo duong dan hien tai
    var path = window.location.pathname.toLowerCase();
    $('.navbar-nav .nav-link').each(function () {
        var href = this.getAttribute('href');
        if (!href) return;
        var matched = (href === '/' && path === '/')
            || (href !== '/' && path.indexOf(href.toLowerCase()) === 0)
            || (href.toLowerCase().indexOf('/home') === 0 && (path === '/' || path.indexOf('/home') === 0));
        if (matched) $(this).addClass('active');
    });

    // Dark mode toggle
    function syncThemeIcon() {
        var isDark = document.documentElement.getAttribute('data-bs-theme') === 'dark';
        $('#themeToggleIcon').attr('class', isDark ? 'bi bi-sun-fill text-warning' : 'bi bi-moon-stars');
    }

    syncThemeIcon();

    $('#themeToggleBtn').on('click', function () {
        var current = document.documentElement.getAttribute('data-bs-theme') || 'light';
        var next = current === 'dark' ? 'light' : 'dark';
        document.documentElement.setAttribute('data-bs-theme', next);
        localStorage.setItem('qlthitn_theme', next);
        syncThemeIcon();
    });

    // Dam bao toan bo chu cua cau hoi hien thi lien mach o tren, anh luon nam rieng ben duoi
    function repositionQuestionImages() {
        $('.question-card').each(function () {
            var $card = $(this);
            var $content = $card.find('.question-content').first();
            if ($content.length === 0) return;

            var $inlineImgs = $content.find('img, figure');
            if ($inlineImgs.length === 0) return;

            var $imgWrap = $card.find('.question-images').first();
            if ($imgWrap.length === 0) {
                $imgWrap = $('<div class="question-images my-3"></div>');
                $content.after($imgWrap);
            }

            $inlineImgs.each(function () {
                var $elem = $(this);
                if ($elem.is('figure')) {
                    $imgWrap.append($elem);
                } else if ($elem.parent('figure').length === 0) {
                    var $fig = $('<figure class="question-image"></figure>');
                    $fig.append($elem);
                    $imgWrap.append($fig);
                }
            });
        });
    }

    repositionQuestionImages();

    // -------------------------------------------------------------
    // Sticky Navbar + Glassmorphism on Scroll
    // -------------------------------------------------------------
    var $navbar = $('.main-nav');
    function handleNavbarScroll() {
        if ($(window).scrollTop() > 24) {
            $navbar.addClass('nav-scrolled');
        } else {
            $navbar.removeClass('nav-scrolled');
        }
    }
    $(window).on('scroll', handleNavbarScroll);
    handleNavbarScroll();

    // -------------------------------------------------------------
    // Scroll Reveal + Stagger Animation
    // -------------------------------------------------------------
    function initScrollReveal() {
        if (window.matchMedia && window.matchMedia('(prefers-reduced-motion: reduce)').matches) {
            $('.reveal, .reveal-up, .reveal-scale, .stagger-item').addClass('revealed');
            return;
        }

        if (!('IntersectionObserver' in window)) {
            $('.reveal, .reveal-up, .reveal-scale, .stagger-item').addClass('revealed');
            return;
        }

        var observerOptions = {
            root: null,
            rootMargin: '0px 0px -40px 0px',
            threshold: 0.08
        };

        var revealObserver = new IntersectionObserver(function (entries, observer) {
            entries.forEach(function (entry) {
                if (entry.isIntersecting) {
                    var $el = $(entry.target);
                    if ($el.hasClass('stagger-group')) {
                        var $items = $el.find('.stagger-item');
                        $items.each(function (idx) {
                            var item = this;
                            setTimeout(function () {
                                $(item).addClass('revealed');
                            }, idx * 110);
                        });
                        $el.addClass('revealed');
                    } else {
                        $el.addClass('revealed');
                    }
                    observer.unobserve(entry.target);
                }
            });
        }, observerOptions);

        $('.reveal, .reveal-up, .reveal-scale, .stagger-group').each(function () {
            revealObserver.observe(this);
        });
    }

    initScrollReveal();
});
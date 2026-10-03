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
});
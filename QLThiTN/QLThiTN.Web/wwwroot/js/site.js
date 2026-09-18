// Hệ thống ôn thi THPT Quốc gia - Site JS

$(function () {
    // Tự động ẩn alert sau 4 giây
    window.setTimeout(function () {
        $('.alert-dismissible').fadeOut('slow');
    }, 4000);
});

// Đồng hồ đếm ngược cho trang làm bài thi
var examTimer = (function () {
    var intervalId = null;
    var endTime = null;

    function start(durationSeconds) {
        if (intervalId) return;
        endTime = new Date(new Date().getTime() + durationSeconds * 1000);
        intervalId = window.setInterval(tick, 1000);
        tick();
    }

    function tick() {
        if (!endTime) return;
        var remaining = Math.max(0, Math.floor((endTime - new Date()) / 1000));
        var hours = Math.floor(remaining / 3600);
        var minutes = Math.floor((remaining % 3600) / 60);
        var seconds = remaining % 60;

        var display = $('.timer-box').first();
        if (display.length === 0) {
            stop();
            return;
        }

        var text = '';
        if (hours > 0) text += (hours < 10 ? '0' : '') + hours + ':';
        text += (minutes < 10 ? '0' : '') + minutes + ':';
        text += (seconds < 10 ? '0' : '') + seconds;
        display.text(text);

        if (remaining <= 300) {
            display.addClass('critical');
        }

        if (remaining <= 0) {
            stop();
            $('#examForm').submit();
        }
    }

    function stop() {
        if (intervalId) {
            window.clearInterval(intervalId);
            intervalId = null;
        }
    }

    return { start: start, stop: stop };
})();
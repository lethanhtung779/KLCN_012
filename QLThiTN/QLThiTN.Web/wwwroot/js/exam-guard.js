(function (root) {
    'use strict';
    class ExamGuard {
        constructor(options) {
            this.options = options;
            this.leaving = false;
            this.wasHidden = false;
            this.awayCount = 0;
        }
        active() { return !this.leaving && !this.options.isSubmitting(); }
        beforeUnload(event) {
            if (!this.active()) return;
            event.preventDefault();
            event.returnValue = '';
        }
        visibility(hidden) {
            if (!this.active()) return;
            if (hidden && !this.wasHidden) {
                this.wasHidden = true;
                this.awayCount++;
                this.options.save();
                this.options.record('hidden');
            } else if (!hidden && this.wasHidden) {
                this.wasHidden = false;
                this.options.record('visible');
                this.options.notice('Bạn vừa rời màn hình thi (' + this.awayCount +
                    ' lần trong lần mở trang này). Thời gian làm bài vẫn tiếp tục chạy. ' +
                    'Hệ thống ghi nhận sự kiện khi có kết nối; đây không phải kết luận gian lận.');
            }
        }
        async navigate(url) {
            if (!this.active() || this.navigating) return;
            this.navigating = true;
            try {
            if (!this.options.confirm('Bạn chưa nộp bài. Thời gian vẫn tiếp tục chạy khi rời trang. Bạn có muốn rời bài thi?')) return;
            const saved = await this.options.save();
            if (!this.active()) return;
            if (!saved && !this.options.confirm('Chưa lưu được đáp án mới nhất. Rời trang có thể mất các thay đổi chưa lưu. Bạn vẫn muốn rời đi?')) return;
            this.options.record('leave');
            this.leaving = true;
            this.options.navigate(url);
            } finally { this.navigating = false; }
        }
    }
    root.ExamGuard = ExamGuard;
    if (typeof module !== 'undefined') module.exports = ExamGuard;
})(typeof window !== 'undefined' ? window : globalThis);

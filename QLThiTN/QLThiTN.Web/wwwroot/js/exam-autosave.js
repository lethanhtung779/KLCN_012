(function (root) {
    'use strict';
    class ExamAutosave {
        constructor(save, report) {
            this.save = save;
            this.report = report;
            this.revision = 0;
            this.savedRevision = 0;
            this.pending = null;
        }
        get dirty() { return this.revision !== this.savedRevision; }
        markDirty() {
            this.revision++;
            this.report('pending');
        }
        flush() {
            if (this.pending) return this.pending;
            if (!this.dirty) return Promise.resolve(true);
            const revision = this.revision;
            this.report('saving');
            this.pending = Promise.resolve().then(() => this.save()).then(result => {
                if (!result || !result.success) throw new Error(result?.message || 'Không lưu được bài. Hệ thống sẽ thử lại.');
                this.savedRevision = revision;
                this.report(this.dirty ? 'pending' : 'saved', result.luuLuc);
                return true;
            }).catch(error => {
                // Keep all unacknowledged changes dirty, including edits made in flight.
                this.report('error', error.message);
                return false;
            }).finally(() => { this.pending = null; });
            return this.pending;
        }
    }
    root.ExamAutosave = ExamAutosave;
    if (typeof module !== 'undefined') module.exports = ExamAutosave;
})(typeof window !== 'undefined' ? window : globalThis);

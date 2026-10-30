// Render cong thuc LaTeX trong noi dung cau hoi / phuong an.
// Su dung KaTeX (nhanh, nhe, an toan), khong block main thread hoac gay vong lap vo tan.
(function () {
    'use strict';

    var DELIM = [
        { left: '$$', right: '$$', display: true },
        { left: '\\[', right: '\\]', display: true },
        { left: '$', right: '$', display: false },
        { left: '\\(', right: '\\)', display: false }
    ];

    function katexReady() {
        return !!(window.katex && window.renderMathInElement);
    }

    function renderWithKatex(root) {
        if (!root || !katexReady()) return;
        try {
            window.renderMathInElement(root, {
                delimiters: DELIM,
                throwOnError: false,
                errorColor: '#c0392b',
                strict: false,
                trust: false
            });
        } catch (e) {
            console.warn('[latex] KaTeX render loi:', e);
        }
    }

    function render(root) {
        root = root || document.body;
        if (!root) return;

        if (katexReady()) {
            renderWithKatex(root);
        } else {
            // KaTeX duoc nap voi defer hoac async -> doi su kien san sang
            var rendered = false;
            var safeRender = function () {
                if (rendered) return;
                if (katexReady()) {
                    rendered = true;
                    renderWithKatex(root);
                }
            };

            document.addEventListener('DOMContentLoaded', safeRender);
            window.addEventListener('load', safeRender);

            // Thu lai sau mot khoang ngan phong truong hop CDN tra ve muon
            setTimeout(safeRender, 500);
            setTimeout(safeRender, 1500);
            setTimeout(safeRender, 3000);
        }
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', function () { render(document.body); });
    } else {
        render(document.body);
    }

    // Phuc vu cho trang goi render lai sau khi du lieu AJAX ve noi (neu co).
    window.KLRenderLatex = render;
})();

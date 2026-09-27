// Render cong thuc LaTeX trong noi dung cau hoi / phuong an.
//
// Chien luoc: thu KaTeX truoc (nhanh, nhe). Cong thuc nao KaTeX khong render
// duoc (vi du \xrightarrow, \begin{array} phuc tap) se duoc tach ra truoc,
// danh dau, cho KaTeX bo qua, roi MathJax xu ly lai - dung chuyen giao
// "tu chon theo do kho" ma khong can tai ca hai thu vien cung luc.
//
// Luu y: phai tach node KHO ra TRUOC khi goi KaTeX. Neu de KaTeX chay
// truoc, no se bien doi node do thanh span loi (throwOnError:false) va
// MathJax se khong con gi de render.
(function () {
    'use strict';

    var DELIM = [
        { left: '$$', right: '$$', display: true },
        { left: '\\[', right: '\\]', display: true },
        { left: '$', right: '$', display: false },
        { left: '\\(', right: '\\)', display: false }
    ];

    // Mot bieu thuc co "lenh" MaTeX phuc tap -> danh dau cho MathJax xu ly.
    var HARD = /\\(xrightarrow|begin\{|end\{|overset|underset|substack|matrix|vmatrix|bmatrix|binom|cases|aligned|split|stackrel)/;

    var MARK_ATTR = 'data-kl-mj';
    var MARK_TEXT = 'KLMJ' + (Math.random() * 1e9 | 0);

    function katexReady() {
        return !!(window.katex && window.renderMathInElement);
    }

    function mathjaxReady() {
        return !!(window.MathJax && window.MathJax.typesetPromise);
    }

    // Tach cac bieu thuc kho ra khoi DOM, thay bang <span> rong co danh dau.
    // Tra ve so luong da tach (0 = khong co gi can MathJax).
    function extractHard(root) {
        var hard = [];
        var walker = document.createTreeWalker(root, NodeFilter.SHOW_TEXT, null);
        var node;
        while ((node = walker.nextNode())) {
            var t = node.nodeValue;
            if (!t || t.indexOf('$') === -1) continue;
            if (!HARD.test(t)) continue;

            var span = document.createElement('span');
            span.setAttribute(MARK_ATTR, MARK_TEXT);
            // Giu nguyen toan bo noi dung ban goc, phan text con lai cua node
            // van duoc giu o node goc de KaTeX xu ly binh thuong.
            var idx = t.search(HARD);
            if (idx > 0) node.nodeValue = t.slice(0, idx);
            span.textContent = t.slice(idx);
            if (node.parentNode) node.parentNode.insertBefore(span, node.nextSibling);

            // Node goc bay khong con ky tu '$' nen bo qua o cac vong sau.
            if (!node.nodeValue) node.parentNode.removeChild(node);
            hard.push(span);
        }
        return hard.length;
    }

    function renderWithKatex(root) {
        try {
            window.renderMathInElement(root, {
                delimiters: DELIM,
                throwOnError: false,
                errorColor: '#c0392b',
                strict: false,
                trust: false
            });
        } catch (e) {
            console.warn('[latex] KaTeX that bai:', e);
        }
    }

    function loadMathJax() {
        return new Promise(function (resolve) {
            if (mathjaxReady()) return resolve();
            var s = document.createElement('script');
            s.src = 'https://cdn.jsdelivr.net/npm/mathjax@3.2.2/es5/tex-mml-chtml.js';
            s.async = true;
            s.onload = function () { resolve(); };
            s.onerror = function () { resolve(); };
            document.head.appendChild(s);
        });
    }

    function render(root) {
        if (!root) return;

        // 1. tach bieu thuc kho ra TRUOC khi KaTeX bien doi DOM.
        var nHard = extractHard(root);

        // 2. KaTeX render phan con lai (bieu thuc don gian).
        if (katexReady()) {
            renderWithKatex(root);
        } else {
            window.addEventListener('load', function () {
                if (katexReady()) renderWithKatex(root);
            });
        }

        if (nHard === 0) return;

        // 3. Chi nap MathJax khi that su co bieu thuc kho.
        loadMathJax().then(function () {
            if (!mathjaxReady()) return;
            window.MathJax.typesetPromise()
                .then(function () {
                    // Bo danh dau de render lai khong tach nham ve sau.
                    var spans = root.querySelectorAll('[' + MARK_ATTR + ']');
                    for (var i = 0; i < spans.length; i++) {
                        spans[i].removeAttribute(MARK_ATTR);
                    }
                })
                .catch(function (e) { console.warn('[latex] MathJax that bai:', e); });
        });
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', function () { render(document.body); });
    } else {
        render(document.body);
    }

    // Phuc vu cho trang goi render lai sau khi du lieu AJAX ve noi (neu co).
    window.KLRenderLatex = render;
})();

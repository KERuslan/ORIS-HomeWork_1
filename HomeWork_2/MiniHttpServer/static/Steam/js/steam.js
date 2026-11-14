
(function () {
  'use strict';

  function showMessage(form, text) {
    var box = form.querySelector('.local-form-message');
    if (!box) {
      box = document.createElement('div');
      box.className = 'local-form-message';
      box.style.cssText = 'color:#e3524b;margin:8px 0;font-size:13px;';
      form.insertBefore(box, form.firstChild);
    }
    box.textContent = text;
  }

  function init() {
    document.querySelectorAll('[role="checkbox"]').forEach(function (box) {
      function toggle() {
        var on = box.getAttribute('aria-checked') === 'true';
        box.setAttribute('aria-checked', on ? 'false' : 'true');
        box.classList.toggle('checked', !on);
      }
      box.addEventListener('click', toggle);
      box.addEventListener('keydown', function (e) {
        if (e.key === ' ' || e.key === 'Enter') { e.preventDefault(); toggle(); }
      });
    });

    var loginForm = document.querySelector('.login_featuretarget_ctn form');
    if (loginForm) {
      loginForm.addEventListener('submit', function (e) {
        e.preventDefault();
        var inputs = loginForm.querySelectorAll('input');
        var login = inputs[0] ? inputs[0].value.trim() : '';
        var pass = inputs[1] ? inputs[1].value : '';
        if (!login || !pass) {
          showMessage(loginForm, 'Введите имя аккаунта и пароль.');
          return;
        }
        showMessage(loginForm, 'Демо-страница: вход не выполняется.');
      });
    }
  }

  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', init);
  } else {
    init();
  }
})();

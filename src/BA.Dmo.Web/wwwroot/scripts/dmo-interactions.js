/* ============================================================
   BA DMO — dmo-interactions.js (U-08)
   Canonical interaction contract transcribed from the
   Design-Reference (dmo-interactions.js + DMO §13/§15/§26):
   - lists: one click SELECTS a single row; double click OPENS;
     rows are keyboard focusable; no functional shortcuts;
     events dmo:list-select / dmo:list-open bubble with data-id.
   - calendars: one click selects/filters the day (aria-pressed);
     month changes never auto-select; event dmo:date-select.
   - password reveal (reference login): local visibility only.
   No domain logic lives here (DMO §26).
   ============================================================ */
(function () {
  var SELECTED = "selected";

  function rows(list) {
    return list.querySelectorAll("[data-dmo-row]");
  }

  function selectRow(list, row) {
    rows(list).forEach(function (item) {
      item.classList.toggle(SELECTED, item === row);
      item.setAttribute("aria-selected", item === row ? "true" : "false");
    });
    list.dispatchEvent(new CustomEvent("dmo:list-select", {
      bubbles: true,
      detail: { id: row.dataset.id || null, row: row }
    }));
  }

  document.querySelectorAll("[data-dmo-list]").forEach(function (list) {
    list.setAttribute("role", "listbox");
    rows(list).forEach(function (row) {
      row.setAttribute("role", "option");
      row.tabIndex = 0;
      row.addEventListener("click", function () { selectRow(list, row); });
      row.addEventListener("dblclick", function () {
        list.dispatchEvent(new CustomEvent("dmo:list-open", {
          bubbles: true,
          detail: { id: row.dataset.id || null, row: row }
        }));
      });
    });
  });

  document.querySelectorAll("[data-dmo-calendar]").forEach(function (calendar) {
    calendar.addEventListener("click", function (event) {
      var day = event.target.closest("[data-date]");
      if (!day || day.disabled) { return; }
      calendar.querySelectorAll("[data-date]").forEach(function (item) {
        item.classList.toggle(SELECTED, item === day);
        item.setAttribute("aria-pressed", item === day ? "true" : "false");
      });
      calendar.dispatchEvent(new CustomEvent("dmo:date-select", {
        bubbles: true,
        detail: { date: day.dataset.date }
      }));
    });
  });

  /* Reference login password reveal: local visibility only. */
  document.querySelectorAll("[data-dmo-password-toggle]").forEach(function (toggle) {
    var target = document.getElementById(toggle.getAttribute("data-dmo-password-toggle"));
    if (!target) { return; }
    toggle.addEventListener("click", function () {
      var show = target.type === "password";
      target.type = show ? "text" : "password";
      toggle.textContent = show
        ? toggle.getAttribute("data-label-hide") || "Ocultar"
        : toggle.getAttribute("data-label-show") || "Mostrar";
    });
  });

  /* Double click opens the record (DMO §13): rows carrying data-open-url
     navigate on dmo:list-open. Isolated bridge for server-rendered pages. */
  document.addEventListener("dmo:list-open", function (event) {
    var row = event.detail && event.detail.row;
    var url = row && row.getAttribute("data-open-url");
    if (url) { window.location.assign(url); }
  });
})();

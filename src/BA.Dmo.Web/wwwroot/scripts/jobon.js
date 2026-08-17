/* ============================================================
   BA DMO — jobon.js (U-13)
   Job On page behavior per portal-dmo-design-final/job-on-v48-folha-producao.html.
   
   Contract:
   - Tab navigation (planeamento/folha/historico/definicoes)
   - Mode toggle (consulta ↔ edição) for sheet editing
   - Image handling via File System Access API placeholder
   - Calendar integration for planning tab (dmo:date-select)
   - Tool "Alterar" inventory picker open/close
   - CAL rows add/remove in editing mode
   
   No domain logic; purely presentational interaction.
   ============================================================ */
(function () {
  const qs = (s, r = document) => r.querySelector(s);
  const qsa = (s, r = document) => [...r.querySelectorAll(s)];

  // Tab switching
  function openView(viewId) {
    qsa(".tab").forEach(tab => {
      const link = tab.querySelector("a");
      if (link && link.href.includes("tab=")) {
        const href = new URL(link.href);
        tab.classList.toggle("nav-tab--active", href.searchParams.get("tab") === viewId);
      }
    });

    qsa(".app-view").forEach(v => v.classList.toggle("app-view--active", v.id === `view-${viewId}`));
    window.scrollTo({ top: 0, behavior: "smooth" });
  }

  // Initialize tab listeners
  qsa('.nav-area .nav-tab a[href*="tab=""]').forEach(tabLink => {
    tabLink.onclick = e => {
      e.preventDefault();
      const href = new URL(tabLink.href);
      const viewId = href.searchParams.get("tab") || "folha";
      openView(viewId);
    };
  });

  // Sheet mode toggle (GLM-JOB-04)
  const sheet = qs("#jobSheet");
  const editBtn = qs("#edit-sheet-btn");
  const saveBtn = qs("#save-sheet-btn");
  const sheetMode = qs("#sheet-mode");

  if (editBtn && saveBtn && sheetMode) {
    editBtn.onclick = () => {
      const editing = !sheet.classList.contains("editing");
      editBtn.textContent = editing ? "Cancelar edição" : "Editar folha";
      saveBtn.hidden = !editing;
      sheetMode.textContent = editing ? "Modo edição" : "Modo consulta";
      sheet.classList.toggle("editing", editing);
      
      // Open/closed inventory pickers when exiting edit mode
      if (!editing) {
        qsa(".inventory-picker.open").forEach(p => p.classList.remove("open"));
      }
      
      _syncEditModeToBody();
    };

    saveBtn.onclick = () => {
      // Save action – TODO: U-13 runtime binds actual persistence
      const editing = sheet.classList.contains("editing");
      if (editing) {
        editBtn.click(); // Close edit mode
      }
    };
  }

  function _syncEditModeToBody() {
    const canEdit = sheet?.classList.contains("editing");
    document.body.setAttribute("data-can-edit-jobon", String(canEdit ?? false));
  }

  // Inventory picker (tool "Alterar" button)
  qsa(".tool-title-actions .btn.compact").forEach(btn => {
    btn.onclick = () => {
      const picker = qs("#inventoryPicker");
      if (picker) {
        picker.classList.toggle("open");
        picker.scrollIntoView({ behavior: "smooth", block: "center" });
      }
    };
  });

  // CAL rows management
  const calRows = qs("#cal-rows");
  const addCalRowBtn = qs("#add-cal-row-btn");

  if (addCalRowBtn && calRows) {
    addCalRowBtn.onclick = () => {
      calRows.insertAdjacentHTML("beforeend", `
        <tr data-testid="cal-row">
          <td><input type="text" placeholder="Novo elemento" /></td>
          <td><input type="text" placeholder="Valor" /></td>
          <td><input type="number" placeholder="0" /></td>
          <td class="edit-only"><button type="button" class="btn btn--danger btn--compact cal-remove" data-testid="btn-remove-cal-row">Remover</button></td>
        </tr>`);
    };
  }

  // CAL remove delegation
  if (calRows) {
    calRows.addEventListener("click", e => {
      const removing = sheet?.classList.contains("editing") && e.target.matches(".cal-remove");
      if (removing) {
        e.target.closest("tr")?.remove();
      }
    });
  }

  // Image handling placeholder (TD-23 — FileSystemAccessAPI wired in U-13+)
  const imageInput = qs("#job-image-input");
  const imagePreview = qs("#image-preview");

  if (imageInput && imagePreview) {
    imageInput.onchange = e => {
      const file = e.target.files[0];
      if (!file) return;
      imagePreview.innerHTML = "";
      const img = document.createElement("img");
      img.src = URL.createObjectURL(file);
      img.alt = "Imagem do artigo";
      imagePreview.appendChild(img);
    };
  }

  // Catalog options management (Definições)
  const catalogRows = qs("[data-option-row]");
  const addOptionBtn = qs("#add-option-btn");
  const editOptionBtn = qs("#edit-option-btn");
  const disableOptionBtn = qs("#disable-option-btn");

  if (addOptionBtn) {
    addOptionBtn.onclick = () => {
      const input = qs("#new-option");
      const label = input?.value.trim();
      if (!label) return;
      
      // Check duplicates
      const allRows = qsa("[data-option-row]");
      const existing = qsa("strong", allRows)
        .some(str => str.textContent.toLocaleLowerCase("pt-PT") === label.toLocaleLowerCase("pt-PT"));
      
      if (existing) {
        input.focus();
        return;
      }
      
      // Add row
      const count = allRows.length + 1;
      catalogRows?.insertAdjacentHTML("beforeend", `
        <tr data-option-row>
          <td>${count}</td>
          <td><strong>${label}</strong></td>
          <td><span class="pill pill--good">Ativa</span></td>
          <td>Disponível em novos registos</td>
        </tr>`);
      
      input.value = "";
    };
  }

  // Select catalog row
  if (catalogRows) {
    qsa("[data-option-row]").forEach(row => {
      row.onclick = () => {
        qsa("[data-option-row]").forEach(r => r.classList.remove("selected"));
        row.classList.add("selected");
      };
    });
  }

  _syncEditModeToBody();
})();

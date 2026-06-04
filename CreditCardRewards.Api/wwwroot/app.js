const cardList = document.querySelector("#card-list");
const template = document.querySelector("#card-row-template");
const statusEl = document.querySelector("#status");
const planOutput = document.querySelector("#plan-output");

function addCardRow(values = {}) {
  const fragment = template.content.cloneNode(true);
  const row = fragment.querySelector(".card-row");

  row.querySelector('[name="name"]').value = values.name ?? "";
  row.querySelector('[name="issuer"]').value = values.issuer ?? "";
  row.querySelector('[name="totalLimit"]').value = values.totalLimit ?? "";

  row.querySelector(".remove-card").addEventListener("click", () => {
    if (cardList.children.length > 1) {
      row.remove();
    }
  });

  cardList.appendChild(fragment);
}

function getCardsFromForm() {
  return [...cardList.querySelectorAll(".card-row")].map((row) => ({
    name: row.querySelector('[name="name"]').value.trim(),
    issuer: row.querySelector('[name="issuer"]').value.trim() || null,
    totalLimit: Number(row.querySelector('[name="totalLimit"]').value)
  }));
}

function setStatus(message) {
  statusEl.textContent = message;
}

function renderPlan(plan) {
  if (!plan.length) {
    planOutput.className = "empty-state";
    planOutput.textContent = "No cards are saved yet. Add your cards first.";
    return;
  }

  planOutput.className = "plan-list";
  planOutput.innerHTML = plan.map((item) => `
    <article class="plan-item">
      <h3>${escapeHtml(item.cardName)}</h3>
      <p>${escapeHtml(item.issuer)} issuer data, ${item.existingActiveOffers} active offers already saved.</p>
      <ul>
        ${item.sourceCandidates.map((source) => `
          <li><strong>${escapeHtml(source.label)}:</strong> ${escapeHtml(source.query)}</li>
        `).join("")}
      </ul>
    </article>
  `).join("");
}

function escapeHtml(value) {
  return String(value)
    .replaceAll("&", "&amp;")
    .replaceAll("<", "&lt;")
    .replaceAll(">", "&gt;")
    .replaceAll('"', "&quot;")
    .replaceAll("'", "&#039;");
}

async function refreshPlan() {
  setStatus("Building offer refresh plan...");

  const response = await fetch("/api/cards/offers/refresh-plan", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: "[]"
  });

  if (!response.ok) {
    const message = await response.text();
    throw new Error(message || "Could not build the offer refresh plan.");
  }

  renderPlan(await response.json());
  setStatus("Offer plan ready.");
}

document.querySelector("#add-card").addEventListener("click", () => addCardRow());

document.querySelector("#refresh-plan").addEventListener("click", async () => {
  try {
    await refreshPlan();
  } catch (error) {
    setStatus(error.message);
  }
});

document.querySelector("#cards-form").addEventListener("submit", async (event) => {
  event.preventDefault();
  setStatus("Saving cards...");

  const cards = getCardsFromForm();
  const response = await fetch("/api/cards/onboarding", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ cards })
  });

  if (!response.ok) {
    setStatus("Could not save cards. Check the entered names and limits.");
    return;
  }

  setStatus("Cards saved. Finding relevant offer sources...");

  try {
    await refreshPlan();
  } catch (error) {
    setStatus(error.message);
  }
});

addCardRow({ name: "HDFC Infinia", issuer: "HDFC", totalLimit: 500000 });

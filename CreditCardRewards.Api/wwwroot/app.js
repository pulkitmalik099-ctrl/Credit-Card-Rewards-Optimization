const cardList = document.querySelector("#card-list");
const template = document.querySelector("#card-row-template");
const statusEl = document.querySelector("#status");
const planOutput = document.querySelector("#plan-output");
const recommendStatusEl = document.querySelector("#recommend-status");
const recommendOutput = document.querySelector("#recommend-output");
const portfolioPanel = document.querySelector("#portfolio-panel");
const recommendPanel = document.querySelector("#recommend-panel");

const currencyFormatter = new Intl.NumberFormat("en-IN", {
  style: "currency",
  currency: "INR",
  maximumFractionDigits: 2
});

const numberFormatter = new Intl.NumberFormat("en-IN", {
  maximumFractionDigits: 2
});

function addCardRow(values = {}) {
  const fragment = template.content.cloneNode(true);
  const row = fragment.querySelector(".card-row");

  row.querySelector('[name="name"]').value = values.name ?? "";
  row.querySelector('[name="issuer"]').value = values.issuer ?? "";
  row.querySelector('[name="totalLimit"]').value = values.totalLimit ?? "";
  row.querySelector('[name="baseRewardRate"]').value = values.baseRewardRate ?? "";
  row.querySelector('[name="baseRewardPointValue"]').value = values.baseRewardPointValue ?? "";
  row.querySelector('[name="accumulatedSpend"]').value = values.accumulatedSpend ?? "";
  row.querySelector('[name="accumulatedRewardPoints"]').value = values.accumulatedRewardPoints ?? "";

  row.querySelector(".remove-card").addEventListener("click", () => {
    if (cardList.children.length > 1) {
      row.remove();
    }
  });

  cardList.appendChild(fragment);
}

function getCardsFromForm() {
  return [...cardList.querySelectorAll(".card-row")].map((row) => {
    const baseRewardRate = row.querySelector('[name="baseRewardRate"]').value;
    const baseRewardPointValue = row.querySelector('[name="baseRewardPointValue"]').value;
    const accumulatedSpend = row.querySelector('[name="accumulatedSpend"]').value;
    const accumulatedRewardPoints = row.querySelector('[name="accumulatedRewardPoints"]').value;

    return {
      name: row.querySelector('[name="name"]').value.trim(),
      issuer: row.querySelector('[name="issuer"]').value.trim() || null,
      totalLimit: Number(row.querySelector('[name="totalLimit"]').value),
      baseRewardRate: baseRewardRate ? Number(baseRewardRate) : null,
      baseRewardPointValue: baseRewardPointValue ? Number(baseRewardPointValue) : null,
      accumulatedSpend: accumulatedSpend ? Number(accumulatedSpend) : 0,
      accumulatedRewardPoints: accumulatedRewardPoints ? Number(accumulatedRewardPoints) : 0
    };
  });
}

function setStatus(message) {
  statusEl.textContent = message;
}

function setRecommendStatus(message) {
  recommendStatusEl.textContent = message;
}

function escapeHtml(value) {
  return String(value)
    .replaceAll("&", "&amp;")
    .replaceAll("<", "&lt;")
    .replaceAll(">", "&gt;")
    .replaceAll('"', "&quot;")
    .replaceAll("'", "&#039;");
}

function formatCurrency(value) {
  return currencyFormatter.format(Number(value) || 0);
}

function formatRewardBreakdown(details) {
  const parts = [];

  if (details.pointsEarned > 0) {
    parts.push(`${numberFormatter.format(details.pointsEarned)} reward points`);
  }

  if (details.cashbackEarned > 0) {
    parts.push(`${formatCurrency(details.cashbackEarned)} cashback value`);
  }

  if (details.milestoneContributionValue > 0) {
    parts.push(`${formatCurrency(details.milestoneContributionValue)} milestone progress`);
  }

  if (details.feeWaiverContributionValue > 0) {
    parts.push(`${formatCurrency(details.feeWaiverContributionValue)} fee waiver progress`);
  }

  if (!parts.length) {
    return "No direct rewards configured for this card yet.";
  }

  return parts.join(" · ");
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

function renderRecommendations(recommendations, expense) {
  if (!recommendations.length) {
    recommendOutput.className = "empty-state";
    recommendOutput.textContent = "No recommendations returned. Save your cards first on the My cards tab.";
    return;
  }

  const best = recommendations[0];

  recommendOutput.className = "recommendation-list";
  recommendOutput.innerHTML = `
    <article class="best-pick">
      <p class="best-pick-label">Use this card</p>
      <h3>${escapeHtml(best.cardName)}</h3>
      <p class="best-pick-value">${formatCurrency(best.rewardValue)} total estimated value</p>
      <p class="best-pick-return">${numberFormatter.format(best.effectiveReturnPercentage)}% effective return on ${formatCurrency(expense.amount)}</p>
      <p class="best-pick-detail">${escapeHtml(formatRewardBreakdown(best.details))}</p>
      <p class="best-pick-reason">${escapeHtml(best.reasoning || best.details?.reasoning || "")}</p>
    </article>

    <div class="ranked-list">
      <h3>All cards ranked</h3>
      ${recommendations.map((item) => `
        <article class="rank-item ${item.rank === 1 ? "is-top" : ""}">
          <div class="rank-badge">#${item.rank}</div>
          <div class="rank-body">
            <h4>${escapeHtml(item.cardName)}</h4>
            <p class="rank-value">${formatCurrency(item.rewardValue)} · ${numberFormatter.format(item.effectiveReturnPercentage)}% return</p>
            <p class="rank-breakdown">${escapeHtml(formatRewardBreakdown(item.details))}</p>
            <p class="rank-reason">${escapeHtml(item.reasoning || item.details?.reasoning || "")}</p>
          </div>
        </article>
      `).join("")}
    </div>
  `;
}

function switchTab(tabName) {
  document.querySelectorAll(".tab-button").forEach((button) => {
    button.classList.toggle("is-active", button.dataset.tab === tabName);
  });

  portfolioPanel.classList.toggle("is-active", tabName === "portfolio");
  portfolioPanel.hidden = tabName !== "portfolio";
  recommendPanel.classList.toggle("is-active", tabName === "recommend");
  recommendPanel.hidden = tabName !== "recommend";
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

async function getRecommendations(expense) {
  setRecommendStatus("Comparing your cards...");

  const response = await fetch("/api/recommendations/portfolio", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({
      amount: expense.amount,
      merchant: expense.merchant,
      category: expense.category,
      enableMilestoneMode: expense.enableMilestoneMode
    })
  });

  if (!response.ok) {
    const message = await response.text();
    throw new Error(message || "Could not get recommendations.");
  }

  const recommendations = await response.json();
  renderRecommendations(recommendations, expense);
  setRecommendStatus("Comparison complete.");
}

document.querySelectorAll(".tab-button").forEach((button) => {
  button.addEventListener("click", () => switchTab(button.dataset.tab));
});

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

  setStatus("Cards saved.");

  try {
    await refreshPlan();
  } catch (error) {
    setStatus(error.message);
  }
});

document.querySelector("#recommend-form").addEventListener("submit", async (event) => {
  event.preventDefault();

  const expense = {
    amount: Number(document.querySelector("#expense-amount").value),
    category: document.querySelector("#expense-category").value,
    merchant: document.querySelector("#expense-merchant").value.trim(),
    enableMilestoneMode: document.querySelector("#milestone-mode").checked
  };

  try {
    await getRecommendations(expense);
  } catch (error) {
    setRecommendStatus(error.message);
  }
});

addCardRow({
  name: "HDFC Infinia",
  issuer: "HDFC",
  totalLimit: 500000,
  baseRewardRate: 3.3,
  baseRewardPointValue: 1
});
addCardRow({
  name: "Amazon Pay ICICI",
  issuer: "ICICI",
  totalLimit: 200000,
  baseRewardRate: 5,
  baseRewardPointValue: 1
});

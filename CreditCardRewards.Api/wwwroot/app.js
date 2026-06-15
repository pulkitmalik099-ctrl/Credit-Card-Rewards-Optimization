const cardList = document.querySelector("#card-list");
const template = document.querySelector("#card-row-template");
const statusEl = document.querySelector("#status");
const planOutput = document.querySelector("#plan-output");
const recommendStatusEl = document.querySelector("#recommend-status");
const recommendOutput = document.querySelector("#recommend-output");
const portfolioPanel = document.querySelector("#portfolio-panel");
const recommendPanel = document.querySelector("#recommend-panel");

let activeUserProfile = null;

// DOM Elements for user profiles
const profileOverlay = document.querySelector("#profile-overlay");
const profileForm = document.querySelector("#profile-form");
const profileList = document.querySelector("#profile-list");
const existingProfilesSection = document.querySelector("#existing-profiles-section");
const activeProfileWidget = document.querySelector("#active-profile-widget");
const widgetName = document.querySelector("#widget-name");
const widgetEmail = document.querySelector("#widget-email");
const widgetAvatar = document.querySelector("#widget-avatar");
const switchProfileBtn = document.querySelector("#switch-profile-btn");

const currencyFormatter = new Intl.NumberFormat("en-IN", {
  style: "currency",
  currency: "INR",
  maximumFractionDigits: 0
});

const numberFormatter = new Intl.NumberFormat("en-IN", {
  maximumFractionDigits: 2
});

function addCardRow(values = {}) {
  const fragment = template.content.cloneNode(true);
  const row = fragment.querySelector(".card-row");

  row.dataset.id = values.id ?? "";
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
      id: row.dataset.id || null,
      name: row.querySelector('[name="name"]').value.trim(),
      issuer: row.querySelector('[name="issuer"]').value.trim() || null,
      totalLimit: Number(row.querySelector('[name="totalLimit"]').value),
      baseRewardRate: baseRewardRate ? Number(baseRewardRate) : null,
      baseRewardPointValue: baseRewardPointValue ? Number(baseRewardPointValue) : null,
      accumulatedSpend: accumulatedSpend ? Number(accumulatedSpend) : 0,
      accumulatedRewardPoints: accumulatedRewardPoints ? Number(accumulatedRewardPoints) : 0,
      userProfileId: activeUserProfile ? activeUserProfile.id : null
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
  if (value === null || value === undefined) return "";
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
  if (!activeUserProfile) return;
  setStatus("Building offer refresh plan...");

  const cardIds = [...cardList.querySelectorAll(".card-row")]
    .map(row => row.dataset.id)
    .filter(id => id);

  const response = await fetch("/api/cards/offers/refresh-plan", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(cardIds)
  });

  if (!response.ok) {
    const message = await response.text();
    throw new Error(message || "Could not build the offer refresh plan.");
  }

  renderPlan(await response.json());
  setStatus("Offer plan ready.");
}

async function getRecommendations(expense) {
  if (!activeUserProfile) return;
  setRecommendStatus("Comparing your cards...");

  const response = await fetch("/api/recommendations/portfolio", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({
      userProfileId: activeUserProfile.id,
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

// User Profile Functions
async function fetchProfile(id) {
  const response = await fetch(`/api/users/${id}`);
  if (!response.ok) throw new Error("Profile not found");
  return response.json();
}

async function openProfileModal() {
  profileOverlay.classList.add("is-visible");
  
  // Load existing profiles
  try {
    const response = await fetch("/api/users");
    if (response.ok) {
      const profiles = await response.json();
      if (profiles.length > 0) {
        existingProfilesSection.hidden = false;
        profileList.innerHTML = profiles.map(p => `
          <li class="profile-item" data-id="${p.id}">
            <span class="avatar">${escapeHtml(p.name.charAt(0).toUpperCase())}</span>
            <div class="profile-item-details">
              <span class="name">${escapeHtml(p.name)}</span>
              <span class="email">${escapeHtml(p.email)}</span>
            </div>
          </li>
        `).join("");
        
        // Add click listener
        profileList.querySelectorAll(".profile-item").forEach(item => {
          item.addEventListener("click", async () => {
            const id = item.dataset.id;
            activeUserProfile = await fetchProfile(id);
            localStorage.setItem("activeUserProfileId", id);
            profileOverlay.classList.remove("is-visible");
            showProfileWidget(activeUserProfile);
            await loadProfileData();
          });
        });
      } else {
        existingProfilesSection.hidden = true;
      }
    }
  } catch (err) {
    console.error("Could not fetch user profiles", err);
  }
}

function showProfileWidget(profile) {
  activeProfileWidget.hidden = false;
  widgetName.textContent = profile.name;
  widgetEmail.textContent = profile.email;
  widgetAvatar.textContent = profile.name.charAt(0).toUpperCase();
}

async function loadProfileData() {
  cardList.innerHTML = "";
  setStatus("Loading cards...");
  
  try {
    const response = await fetch(`/api/cards?userProfileId=${activeUserProfile.id}`);
    if (response.ok) {
      const cards = await response.json();
      if (cards.length > 0) {
        cards.forEach(card => {
          addCardRow({
            id: card.id,
            name: card.name,
            issuer: card.issuer,
            totalLimit: card.totalLimit,
            baseRewardRate: card.baseRewardRate,
            baseRewardPointValue: card.baseRewardPointValue,
            accumulatedSpend: card.accumulatedSpend,
            accumulatedRewardPoints: card.accumulatedRewardPoints
          });
        });
        
        await refreshPlan();
      } else {
        // No cards saved yet. Seed with default suggestions
        addCardRow({
          name: "HDFC Infinia",
          issuer: "HDFC",
          totalLimit: 500000,
          baseRewardRate: 3.3,
          baseRewardPointValue: 1,
          accumulatedSpend: 0,
          accumulatedRewardPoints: 0
        });
        addCardRow({
          name: "Amazon Pay ICICI",
          issuer: "ICICI",
          totalLimit: 200000,
          baseRewardRate: 5,
          baseRewardPointValue: 1,
          accumulatedSpend: 0,
          accumulatedRewardPoints: 0
        });
        setStatus("Ready to onboarding.");
      }
    }
  } catch (e) {
    console.error("Error loading cards for profile", e);
    setStatus("Failed to load cards.");
  }
}

// Event Listeners
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
  if (!activeUserProfile) return;
  setStatus("Saving cards...");

  const cards = getCardsFromForm();
  const response = await fetch("/api/cards/onboarding", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ 
      userProfileId: activeUserProfile.id,
      cards 
    })
  });

  if (!response.ok) {
    setStatus("Could not save cards. Check the entered names and limits.");
    return;
  }

  const savedCards = await response.json();
  
  // Update rows with returned database IDs
  const rows = cardList.querySelectorAll(".card-row");
  savedCards.forEach((card, index) => {
    if (rows[index]) {
      rows[index].dataset.id = card.id;
    }
  });

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

switchProfileBtn.addEventListener("click", () => {
  openProfileModal();
});

profileForm.addEventListener("submit", async (e) => {
  e.preventDefault();
  const name = document.querySelector("#profile-name").value.trim();
  const email = document.querySelector("#profile-email").value.trim();
  
  try {
    const response = await fetch("/api/users", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ name, email })
    });
    
    if (!response.ok) {
      const msg = await response.text();
      alert(msg || "Could not create user profile");
      return;
    }
    
    activeUserProfile = await response.json();
    localStorage.setItem("activeUserProfileId", activeUserProfile.id);
    profileOverlay.classList.remove("is-visible");
    showProfileWidget(activeUserProfile);
    
    document.querySelector("#profile-name").value = "";
    document.querySelector("#profile-email").value = "";
    
    await loadProfileData();
  } catch (err) {
    alert("Error creating profile: " + err.message);
  }
});

// Load Profile on startup
document.addEventListener("DOMContentLoaded", async () => {
  const savedProfileId = localStorage.getItem("activeUserProfileId");
  if (savedProfileId) {
    try {
      activeUserProfile = await fetchProfile(savedProfileId);
      showProfileWidget(activeUserProfile);
      await loadProfileData();
    } catch (e) {
      console.error("Failed to load saved profile, opening login modal", e);
      localStorage.removeItem("activeUserProfileId");
      openProfileModal();
    }
  } else {
    openProfileModal();
  }
});

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
const loginForm = document.querySelector("#login-form");
const registerForm = document.querySelector("#register-form");
const profileError = document.querySelector("#profile-error");
const loginTabBtn = document.querySelector("#btn-login-tab");
const registerTabBtn = document.querySelector("#btn-register-tab");
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
  
  // Input fields references
  const inputName = row.querySelector('[name="name"]');
  const inputIssuer = row.querySelector('[name="issuer"]');
  const inputLimit = row.querySelector('[name="totalLimit"]');
  const inputRate = row.querySelector('[name="baseRewardRate"]');
  const inputPointValue = row.querySelector('[name="baseRewardPointValue"]');
  const inputSpend = row.querySelector('[name="accumulatedSpend"]');
  const inputPoints = row.querySelector('[name="accumulatedRewardPoints"]');

  // Set initial input values
  inputName.value = values.name ?? "";
  inputIssuer.value = values.issuer ?? "";
  inputLimit.value = values.totalLimit ?? "";
  inputRate.value = values.baseRewardRate ?? "";
  inputPointValue.value = values.baseRewardPointValue ?? "";
  inputSpend.value = values.accumulatedSpend ?? "";
  inputPoints.value = values.accumulatedRewardPoints ?? "";

  // Preview elements references
  const vCard = row.querySelector(".virtual-card");
  const vName = row.querySelector(".virtual-card-name");
  const vIssuer = row.querySelector(".virtual-card-issuer");
  const vLimit = row.querySelector(".virtual-card-limit");
  const vSpend = row.querySelector(".virtual-card-spend");

  // Real-time update helper function
  function updatePreview() {
    vName.textContent = inputName.value.trim() || "Card Name";
    vIssuer.textContent = inputIssuer.value.trim().toUpperCase() || "ISSUER";
    
    const limitVal = Number(inputLimit.value) || 0;
    vLimit.textContent = limitVal > 0 ? formatCurrency(limitVal) : "₹0";

    const spendVal = Number(inputSpend.value) || 0;
    vSpend.textContent = spendVal > 0 ? formatCurrency(spendVal) : "₹0";
  }

  // Bind key/input events for real-time preview updates
  [inputName, inputIssuer, inputLimit, inputSpend].forEach(input => {
    input.addEventListener("input", updatePreview);
  });

  // Theme logic
  let activeTheme = "blue";
  if (values.id) {
    activeTheme = localStorage.getItem("card-theme-" + values.id) || inferCardTheme(values.name, values.issuer);
  } else {
    activeTheme = inferCardTheme(values.name, values.issuer);
  }
  row.dataset.selectedTheme = activeTheme;

  // Apply active theme class
  function applyTheme(themeName) {
    vCard.className = `virtual-card theme-${themeName}`;
    row.dataset.selectedTheme = themeName;
    if (row.dataset.id) {
      localStorage.setItem("card-theme-" + row.dataset.id, themeName);
    }
    
    // Highlight active dot
    row.querySelectorAll(".theme-dot").forEach(dot => {
      dot.classList.toggle("is-active", dot.dataset.theme === themeName);
    });
  }

  applyTheme(activeTheme);
  updatePreview();

  // Bind click listener to theme dots
  row.querySelectorAll(".theme-dot").forEach(dot => {
    dot.addEventListener("click", () => {
      applyTheme(dot.dataset.theme);
    });
  });

  // Auto theme selector based on text typing (only if user hasn't explicitly clicked a theme dot yet)
  let userSelectedThemeExplicitly = false;
  row.querySelectorAll(".theme-dot").forEach(dot => {
    dot.addEventListener("click", () => {
      userSelectedThemeExplicitly = true;
    });
  });

  inputName.addEventListener("change", () => {
    if (!userSelectedThemeExplicitly) {
      const newTheme = inferCardTheme(inputName.value, inputIssuer.value);
      applyTheme(newTheme);
    }
  });
  inputIssuer.addEventListener("change", () => {
    if (!userSelectedThemeExplicitly) {
      const newTheme = inferCardTheme(inputName.value, inputIssuer.value);
      applyTheme(newTheme);
    }
  });

  row.querySelector(".remove-card").addEventListener("click", () => {
    if (cardList.children.length > 1) {
      if (row.dataset.id) {
        localStorage.removeItem("card-theme-" + row.dataset.id);
      }
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

function inferCardTheme(cardName = "", issuer = "") {
  const text = `${cardName} ${issuer}`.toLowerCase();
  if (text.includes("gold") || text.includes("diners") || text.includes("reserve") || text.includes("magnus")) return "gold";
  if (text.includes("amazon") || text.includes("sbi") || text.includes("axis") || text.includes("travel")) return "green";
  if (text.includes("infinia") || text.includes("black") || text.includes("metal") || text.includes("regalia") || text.includes("saphire")) return "dark";
  if (text.includes("onecard") || text.includes("neon") || text.includes("millennia") || text.includes("purple") || text.includes("coral")) return "purple";
  return "blue"; // Default
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
  
  // Find or infer theme for the best card
  let bestCardTheme = "blue";
  if (best.cardId) {
    bestCardTheme = localStorage.getItem("card-theme-" + best.cardId) || inferCardTheme(best.cardName, best.issuer);
  } else {
    bestCardTheme = inferCardTheme(best.cardName, best.issuer);
  }

  recommendOutput.className = "recommendation-list";
  recommendOutput.innerHTML = `
    <article class="best-pick">
      <!-- Virtual Card on the left -->
      <div class="best-pick-card-wrapper">
        <div class="virtual-card large theme-${bestCardTheme} glowing-glow">
          <div class="virtual-card-glass"></div>
          <div class="virtual-card-header">
            <span class="virtual-card-issuer">${escapeHtml(best.issuer || "CARD").toUpperCase()}</span>
            <div class="virtual-card-chip">
              <svg viewBox="0 0 100 100" class="chip-svg">
                <rect width="70" height="50" x="15" y="25" rx="6" fill="#facc15" opacity="0.85"/>
                <path d="M 15 40 H 85 M 15 50 H 85 M 15 60 H 85 M 38 25 V 75 M 62 25 V 75" stroke="rgba(0,0,0,0.2)" stroke-width="1.5"/>
              </svg>
            </div>
          </div>
          <div class="virtual-card-body">
            <h3 class="virtual-card-name">${escapeHtml(best.cardName)}</h3>
          </div>
          <div class="virtual-card-footer">
            <div class="virtual-card-stat">
              <span>RETURN VALUE</span>
              <strong style="color: #4ade80;">${formatCurrency(best.rewardValue)}</strong>
            </div>
            <div class="virtual-card-stat">
              <span>RETURN %</span>
              <strong style="color: #38bdf8;">${numberFormatter.format(best.effectiveReturnPercentage)}%</strong>
            </div>
          </div>
        </div>
      </div>
      
      <!-- Details on the right -->
      <div class="best-pick-details">
        <span class="best-pick-badge">🔥 BEST VALUE OPTION</span>
        <h3>${escapeHtml(best.cardName)}</h3>
        <p class="best-pick-value">${formatCurrency(best.rewardValue)} total estimated value</p>
        <p class="best-pick-return">${numberFormatter.format(best.effectiveReturnPercentage)}% effective return on ${formatCurrency(expense.amount)}</p>
        <p class="best-pick-detail"><strong>Breakdown:</strong> ${escapeHtml(formatRewardBreakdown(best.details))}</p>
        <p class="best-pick-reason"><strong>Why this card?</strong> ${escapeHtml(best.reasoning || best.details?.reasoning || "")}</p>
      </div>
    </article>

    <div class="ranked-list">
      <h3>All cards ranked</h3>
      ${recommendations.map((item) => {
        let cardTheme = "blue";
        if (item.cardId) {
          cardTheme = localStorage.getItem("card-theme-" + item.cardId) || inferCardTheme(item.cardName, item.issuer);
        } else {
          cardTheme = inferCardTheme(item.cardName, item.issuer);
        }
        
        return `
          <article class="rank-item ${item.rank === 1 ? "is-top" : ""}">
            <div class="rank-badge">#${item.rank}</div>
            <div class="rank-body" style="display: flex; gap: 16px; align-items: center; width: 100%;">
              <div class="virtual-card theme-${cardTheme}" style="width: 100px; height: 62px; padding: 6px 8px; border-radius: 6px; font-size: 0.5rem; flex-shrink: 0; box-shadow: 0 4px 10px rgba(0,0,0,0.2);">
                <div class="virtual-card-glass"></div>
                <div style="font-size: 0.35rem; font-weight: 800; opacity: 0.8; text-transform: uppercase; line-height: 1;">${escapeHtml(item.issuer || "CARD").toUpperCase()}</div>
                <div style="font-size: 0.45rem; font-weight: 800; margin-top: 4px; white-space: nowrap; overflow: hidden; text-overflow: ellipsis; line-height: 1.2;">${escapeHtml(item.cardName)}</div>
                <div style="display: flex; justify-content: space-between; align-items: flex-end; margin-top: 4px; font-size: 0.35rem; font-weight: 600; opacity: 0.8; line-height: 1;">
                  <span>VAL: ${formatCurrency(item.rewardValue)}</span>
                  <span>${numberFormatter.format(item.effectiveReturnPercentage)}%</span>
                </div>
              </div>
              <div style="flex-grow: 1;">
                <h4 style="margin: 0 0 4px;">${escapeHtml(item.cardName)}</h4>
                <p class="rank-value">${formatCurrency(item.rewardValue)} · ${numberFormatter.format(item.effectiveReturnPercentage)}% return</p>
                <p class="rank-breakdown">${escapeHtml(formatRewardBreakdown(item.details))}</p>
                <p class="rank-reason">${escapeHtml(item.reasoning || item.details?.reasoning || "")}</p>
              </div>
            </div>
          </article>
        `;
      }).join("")}
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
  switchLandingTab("login");
  
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
      // Save theme to localStorage under the database ID
      const chosenTheme = rows[index].dataset.selectedTheme;
      if (chosenTheme) {
        localStorage.setItem("card-theme-" + card.id, chosenTheme);
      }
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

// Toggling Login / Register Forms
function switchLandingTab(tab) {
  profileError.style.display = "none";
  profileError.textContent = "";
  
  if (tab === "login") {
    loginTabBtn.classList.add("is-active");
    registerTabBtn.classList.remove("is-active");
    loginForm.hidden = false;
    registerForm.hidden = true;
  } else {
    loginTabBtn.classList.remove("is-active");
    registerTabBtn.classList.add("is-active");
    loginForm.hidden = true;
    registerForm.hidden = false;
  }
}

loginTabBtn.addEventListener("click", () => switchLandingTab("login"));
registerTabBtn.addEventListener("click", () => switchLandingTab("register"));

// Email Lookup Login Handler
loginForm.addEventListener("submit", async (e) => {
  e.preventDefault();
  const email = document.querySelector("#login-email").value.trim();
  profileError.style.display = "none";
  profileError.textContent = "";

  try {
    const response = await fetch(`/api/users/email/${encodeURIComponent(email)}`);
    if (!response.ok) {
      profileError.textContent = "No profile found with this email. Go to the 'Create Profile' tab to sign up.";
      profileError.style.display = "block";
      return;
    }

    activeUserProfile = await response.json();
    localStorage.setItem("activeUserProfileId", activeUserProfile.id);
    profileOverlay.classList.remove("is-visible");
    showProfileWidget(activeUserProfile);
    
    document.querySelector("#login-email").value = "";
    await loadProfileData();
  } catch (err) {
    profileError.textContent = "Sign In failed: " + err.message;
    profileError.style.display = "block";
  }
});

// Create Profile Registration Handler
registerForm.addEventListener("submit", async (e) => {
  e.preventDefault();
  const name = document.querySelector("#register-name").value.trim();
  const email = document.querySelector("#register-email").value.trim();
  profileError.style.display = "none";
  profileError.textContent = "";

  try {
    const response = await fetch("/api/users", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ name, email })
    });

    if (!response.ok) {
      const msg = await response.text();
      profileError.textContent = msg || "Could not create user profile.";
      profileError.style.display = "block";
      return;
    }

    activeUserProfile = await response.json();
    localStorage.setItem("activeUserProfileId", activeUserProfile.id);
    profileOverlay.classList.remove("is-visible");
    showProfileWidget(activeUserProfile);
    
    document.querySelector("#register-name").value = "";
    document.querySelector("#register-email").value = "";
    
    await loadProfileData();
  } catch (err) {
    profileError.textContent = "Registration failed: " + err.message;
    profileError.style.display = "block";
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

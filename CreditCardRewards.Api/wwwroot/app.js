const cardList = document.querySelector("#card-list");
const template = document.querySelector("#card-row-template");
const statusEl = document.querySelector("#status");
const planOutput = document.querySelector("#plan-output");
const recommendStatusEl = document.querySelector("#recommend-status");
const recommendOutput = document.querySelector("#recommend-output");
const portfolioPanel = document.querySelector("#portfolio-panel");
const recommendPanel = document.querySelector("#recommend-panel");
const statementsPanel = document.querySelector("#statements-panel");
const advicePanel = document.querySelector("#advice-panel");
const statementsStatus = document.querySelector("#statements-status");
const statementsList = document.querySelector("#statements-list");
const statementModal = document.querySelector("#statement-modal");
const statementCardSelect = document.querySelector("#statement-card-select");
const statementConfirmBtn = document.querySelector("#statement-confirm-btn");
const statementDismissBtn = document.querySelector("#statement-dismiss-btn");
const statementModalStatus = document.querySelector("#statement-modal-status");

let pendingStatementId = null;

let activeUserProfile = null;
let authToken = localStorage.getItem("authToken") || null;

function authHeaders() {
  return authToken
    ? { "Content-Type": "application/json", "Authorization": `Bearer ${authToken}` }
    : { "Content-Type": "application/json" };
}

async function apiFetch(url, options = {}) {
  const res = await fetch(url, {
    ...options,
    headers: { ...authHeaders(), ...(options.headers || {}) }
  });
  if (res.status === 401) {
    authToken = null;
    localStorage.removeItem("authToken");
    localStorage.removeItem("activeUserProfileId");
    openProfileModal();
    throw new Error("Session expired. Please sign in again.");
  }
  return res;
}

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

  // Fetch Rates button
  const fetchBtn = row.querySelector(".fetch-rates-btn");
  const fetchStatus = row.querySelector(".fetch-rates-status");
  const inputRate = row.querySelector('[name="baseRewardRate"]');
  const inputPointValue = row.querySelector('[name="baseRewardPointValue"]');

  fetchBtn.addEventListener("click", async () => {
    const name = inputName.value.trim();
    const issuer = inputIssuer.value.trim();
    if (!name) { fetchStatus.textContent = "Enter card name first."; return; }

    fetchBtn.disabled = true;
    fetchStatus.textContent = "Fetching…";

    try {
      const res = await apiFetch(`/api/cards/lookup?name=${encodeURIComponent(name)}&issuer=${encodeURIComponent(issuer || "")}`);
      if (!res.ok) throw new Error(await res.text());
      const data = await res.json();

      if (data.baseRewardRate) inputRate.value = data.baseRewardRate;
      if (data.baseRewardPointValue) inputPointValue.value = data.baseRewardPointValue;

      const annualFeeInput = row.querySelector('[name="annualFee"]');
      if (annualFeeInput && data.annualFee) annualFeeInput.value = data.annualFee;

      updatePreview();
      fetchStatus.textContent = data.isConfident
        ? `✓ Fetched · ${data.disclaimer}`
        : `⚠ Low confidence — verify manually · ${data.disclaimer}`;
      fetchStatus.style.color = data.isConfident ? "#4ade80" : "#facc15";
    } catch (err) {
      fetchStatus.textContent = "Fetch failed: " + err.message;
      fetchStatus.style.color = "#f87171";
    } finally {
      fetchBtn.disabled = false;
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
  statementsPanel.classList.toggle("is-active", tabName === "statements");
  statementsPanel.hidden = tabName !== "statements";
  advicePanel.classList.toggle("is-active", tabName === "advice");
  advicePanel.hidden = tabName !== "advice";

  if (tabName === "statements") loadPendingStatements();
}

async function refreshPlan() {
  if (!activeUserProfile) return;
  setStatus("Building offer refresh plan...");

  const cardIds = [...cardList.querySelectorAll(".card-row")]
    .map(row => row.dataset.id)
    .filter(id => id);

  const response = await apiFetch("/api/cards/offers/refresh-plan", {
    method: "POST",
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

  const response = await apiFetch("/api/recommendations/portfolio", {
    method: "POST",
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
    const response = await apiFetch(`/api/cards?userProfileId=${activeUserProfile.id}`);
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

        // Load portfolio summary for charts
        try {
          const summaryRes = await apiFetch(`/api/cards/portfolio/summary?userProfileId=${activeUserProfile.id}`);
          if (summaryRes.ok) {
            const summary = await summaryRes.json();
            renderCharts(summary.cards || []);
          }
        } catch (_) {}

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
  const response = await apiFetch("/api/cards/onboarding", {
    method: "POST",
    body: JSON.stringify({ userProfileId: activeUserProfile.id, cards })
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

// JWT Login Handler
loginForm.addEventListener("submit", async (e) => {
  e.preventDefault();
  const email = document.querySelector("#login-email").value.trim();
  const password = document.querySelector("#login-password").value;
  profileError.style.display = "none";
  profileError.textContent = "";

  try {
    const response = await fetch("/api/auth/login", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ email, password })
    });

    if (!response.ok) {
      profileError.textContent = await response.text() || "Sign in failed. Check your email and password.";
      profileError.style.display = "block";
      return;
    }

    const data = await response.json();
    authToken = data.token;
    localStorage.setItem("authToken", authToken);
    activeUserProfile = { id: data.id, name: data.name, email: data.email };
    localStorage.setItem("activeUserProfileId", data.id);
    profileOverlay.classList.remove("is-visible");
    showProfileWidget(activeUserProfile);
    document.querySelector("#login-email").value = "";
    document.querySelector("#login-password").value = "";
    await loadProfileData();
  } catch (err) {
    profileError.textContent = "Sign In failed: " + err.message;
    profileError.style.display = "block";
  }
});

// JWT Registration Handler
registerForm.addEventListener("submit", async (e) => {
  e.preventDefault();
  const name = document.querySelector("#register-name").value.trim();
  const email = document.querySelector("#register-email").value.trim();
  const password = document.querySelector("#register-password").value;
  profileError.style.display = "none";
  profileError.textContent = "";

  try {
    const response = await fetch("/api/auth/register", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ name, email, password })
    });

    if (!response.ok) {
      const msg = await response.text();
      profileError.textContent = msg || "Could not create account.";
      profileError.style.display = "block";
      return;
    }

    const data = await response.json();
    authToken = data.token;
    localStorage.setItem("authToken", authToken);
    activeUserProfile = { id: data.id, name: data.name, email: data.email };
    localStorage.setItem("activeUserProfileId", data.id);
    profileOverlay.classList.remove("is-visible");
    showProfileWidget(activeUserProfile);
    document.querySelector("#register-name").value = "";
    document.querySelector("#register-email").value = "";
    document.querySelector("#register-password").value = "";
    await loadProfileData();
  } catch (err) {
    profileError.textContent = "Registration failed: " + err.message;
    profileError.style.display = "block";
  }
});

// ── Statement Import ─────────────────────────────────────────────────────────

async function loadPendingStatements() {
  statementsStatus.textContent = "Loading…";
  try {
    const res = await apiFetch("/api/statements/pending");
    const pending = await res.json();
    statementsStatus.textContent = "";

    if (!pending.length) {
      statementsList.className = "empty-state";
      statementsList.textContent = "No statements pending review. Upload a PDF or CSV above.";
      return;
    }

    statementsList.className = "statements-pending-list";
    statementsList.innerHTML = pending.map(s => `
      <article class="statement-card" data-id="${escapeHtml(s.id)}">
        <div class="statement-card-header">
          <strong>${escapeHtml(s.fileName)}</strong>
          <span class="statement-badge">${escapeHtml(s.detectedIssuer || "Unknown bank")}</span>
        </div>
        <div class="statement-card-meta">
          ${s.statementPeriodStart ? `Period: ${s.statementPeriodStart.slice(0,10)} → ${(s.statementPeriodEnd || "").slice(0,10)}` : "Period: Unknown"}
          · ${s.transactions.length} transactions · ₹${(s.totalSpend || 0).toLocaleString("en-IN")} spend · ${s.rewardPointsEarned || 0} pts
        </div>
        <div class="statement-card-actions">
          <button type="button" class="primary-button btn-review-statement">Review &amp; Import</button>
        </div>
      </article>
    `).join("");

    statementsList.querySelectorAll(".btn-review-statement").forEach(btn => {
      btn.addEventListener("click", () => {
        const card = btn.closest("[data-id]");
        const stmt = pending.find(s => s.id === card.dataset.id);
        if (stmt) openStatementModal(stmt);
      });
    });
  } catch (err) {
    statementsStatus.textContent = "Failed to load statements: " + err.message;
  }
}

function openStatementModal(statement) {
  pendingStatementId = statement.id;
  const meta = document.querySelector("#statement-modal-meta");
  const txEl = document.querySelector("#statement-modal-transactions");

  meta.innerHTML = `
    <p><strong>File:</strong> ${escapeHtml(statement.fileName)}</p>
    <p><strong>Bank:</strong> ${escapeHtml(statement.detectedIssuer || "Unknown")} &nbsp;|&nbsp; <strong>Card:</strong> ${escapeHtml(statement.detectedCardName || "Unknown")}</p>
    <p><strong>Total spend:</strong> ₹${(statement.totalSpend || 0).toLocaleString("en-IN")} &nbsp;|&nbsp; <strong>Reward points:</strong> ${statement.rewardPointsEarned || 0}</p>
  `;

  txEl.innerHTML = statement.transactions.length
    ? `<table class="tx-table">
        <thead><tr><th>Date</th><th>Merchant</th><th>Amount</th><th>Points</th><th>Category</th></tr></thead>
        <tbody>
          ${statement.transactions.map(tx => `
            <tr>
              <td>${escapeHtml((tx.date || "").slice(0,10))}</td>
              <td>${escapeHtml(tx.merchant)}</td>
              <td>₹${(tx.amount || 0).toLocaleString("en-IN")}</td>
              <td>${tx.rewardPoints || 0}</td>
              <td>${escapeHtml(tx.category || "—")}</td>
            </tr>
          `).join("")}
        </tbody>
      </table>`
    : "<p>No transactions extracted.</p>";

  // Populate card select from the current portfolio
  const rows = cardList.querySelectorAll(".card-row");
  statementCardSelect.innerHTML = '<option value="">-- Select card --</option>';
  rows.forEach(row => {
    const name = row.querySelector('[name="name"]').value;
    const id = row.dataset.id;
    if (id) {
      const opt = document.createElement("option");
      opt.value = id;
      opt.textContent = name;
      statementCardSelect.appendChild(opt);
    }
  });

  statementModalStatus.textContent = "";
  statementModal.hidden = false;
}

document.querySelector("#statement-upload").addEventListener("change", async (e) => {
  const file = e.target.files[0];
  if (!file) return;
  statementsStatus.textContent = `Parsing ${file.name}…`;

  const form = new FormData();
  form.append("file", file);

  try {
    const res = await apiFetch("/api/statements/upload", { method: "POST", body: form, headers: { "Authorization": `Bearer ${authToken}` } });
    if (!res.ok) throw new Error(await res.text());
    statementsStatus.textContent = "Parsed. Check pending list below.";
    await loadPendingStatements();
  } catch (err) {
    statementsStatus.textContent = "Upload failed: " + err.message;
  }

  e.target.value = "";
});

statementConfirmBtn.addEventListener("click", async () => {
  const cardId = statementCardSelect.value;
  if (!cardId) { statementModalStatus.textContent = "Select a card first."; return; }

  statementConfirmBtn.disabled = true;
  statementModalStatus.textContent = "Importing…";

  try {
    const res = await apiFetch(`/api/statements/${pendingStatementId}/confirm?cardId=${cardId}`, { method: "POST" });
    if (!res.ok) throw new Error(await res.text());
    const result = await res.json();
    statementModal.hidden = true;
    statementsStatus.textContent = `✓ Imported ${result.imported} transactions · ${result.rewardPointsAdded} pts added.`;
    await loadPendingStatements();
    await loadProfileData();
  } catch (err) {
    statementModalStatus.textContent = "Import failed: " + err.message;
  } finally {
    statementConfirmBtn.disabled = false;
  }
});

statementDismissBtn.addEventListener("click", async () => {
  if (!pendingStatementId) return;
  await apiFetch(`/api/statements/${pendingStatementId}`, { method: "DELETE" });
  statementModal.hidden = true;
  await loadPendingStatements();
});

// ── Charts ───────────────────────────────────────────────────────────────────

let spendChart = null;
let rewardsChart = null;

function renderCharts(cards) {
  const labels = cards.map(c => c.name);
  const spends = cards.map(c => c.currentYearSpend || 0);
  const rewards = cards.map(c => c.currentYearRewards || 0);
  const palette = ["#3b82f6", "#f59e0b", "#a855f7", "#22c55e", "#64748b",
                   "#ef4444", "#06b6d4", "#f97316", "#14b8a6", "#8b5cf6"];

  const spendCtx = document.querySelector("#spend-chart")?.getContext("2d");
  const rewardsCtx = document.querySelector("#rewards-chart")?.getContext("2d");

  if (!spendCtx || !rewardsCtx) return;

  if (spendChart) spendChart.destroy();
  if (rewardsChart) rewardsChart.destroy();

  const sharedOptions = {
    responsive: true,
    plugins: { legend: { position: "bottom", labels: { color: "#cbd5e1", font: { size: 11 } } } }
  };

  spendChart = new Chart(spendCtx, {
    type: "doughnut",
    data: { labels, datasets: [{ data: spends, backgroundColor: palette, borderWidth: 0 }] },
    options: sharedOptions
  });

  rewardsChart = new Chart(rewardsCtx, {
    type: "bar",
    data: {
      labels,
      datasets: [{
        data: rewards,
        backgroundColor: palette,
        borderRadius: 6
      }]
    },
    options: {
      ...sharedOptions,
      scales: {
        y: { ticks: { color: "#94a3b8", callback: v => "₹" + v.toLocaleString("en-IN") }, grid: { color: "#1e293b" } },
        x: { ticks: { color: "#94a3b8" }, grid: { display: false } }
      },
      plugins: { legend: { display: false } }
    }
  });
}

// ── AI Advisor ────────────────────────────────────────────────────────────────

document.querySelector("#advice-form").addEventListener("submit", async (e) => {
  e.preventDefault();
  if (!activeUserProfile) return;
  const question = document.querySelector("#advice-question").value.trim();
  if (!question) return;

  const statusEl = document.querySelector("#advice-status");
  const outputEl = document.querySelector("#advice-output");
  statusEl.textContent = "Thinking…";
  outputEl.textContent = "";
  outputEl.className = "advice-output";

  try {
    const res = await apiFetch("/api/recommendations/advice", {
      method: "POST",
      body: JSON.stringify({ userProfileId: activeUserProfile.id, question })
    });

    if (!res.ok) throw new Error(await res.text());
    const { advice } = await res.json();
    outputEl.textContent = advice;
    statusEl.textContent = "";
  } catch (err) {
    statusEl.textContent = "Error: " + err.message;
  }
});

// ── Startup ───────────────────────────────────────────────────────────────────

// Load Profile on startup
document.addEventListener("DOMContentLoaded", async () => {
  const savedToken = localStorage.getItem("authToken");
  const savedProfileId = localStorage.getItem("activeUserProfileId");

  if (savedToken && savedProfileId) {
    authToken = savedToken;
    try {
      // Validate token by making an authenticated request
      const res = await apiFetch(`/api/users/${savedProfileId}`);
      if (res.ok) {
        activeUserProfile = await res.json();
        showProfileWidget(activeUserProfile);
        await loadProfileData();
        return;
      }
    } catch (e) {
      console.error("Saved session invalid:", e);
    }
    authToken = null;
    localStorage.removeItem("authToken");
    localStorage.removeItem("activeUserProfileId");
  }

  openProfileModal();
});

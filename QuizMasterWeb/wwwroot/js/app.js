import { api, getSettings, setSettings, getSession, setSession, clearSession, assetUrl, isDemo, setDemo } from './api.js';
import { icon, e, asUtc, dateLabel, completed, quizIcon, typeLabel, isMultiple, empty, field, formError, showFormError, toast, busy } from './ui.js';

const app = document.querySelector('#app');
const modal = document.querySelector('#modal');
const state = { profile: null, quizzes: [], attempts: [], route: '', routeVersion: 0, authVersion: 0, active: null, timer: null, leaderboardVersion: 0, authEmail: '', filter: 'all' };
let returnFocus;
let photoPreview;
let expiryTimer;
let modalVersion = 0;

// Theme preference works on the sign-in screen and every dashboard page.
function themeButton() {
  const light = document.documentElement.dataset.theme === 'light';
  return `<button class="icon-button theme-toggle" data-action="theme" type="button" title="Switch to ${light ? 'dark' : 'light'} mode" aria-label="Switch to ${light ? 'dark' : 'light'} mode">${icon(light ? 'moon' : 'sun')}</button>`;
}
function toggleTheme() {
  const next = document.documentElement.dataset.theme === 'light' ? 'dark' : 'light';
  document.documentElement.dataset.theme = next;
  try { localStorage.setItem('quizmaster-theme', next); } catch {}
  document.querySelectorAll('.theme-toggle').forEach(button => {
    const target=next==='light'?'dark':'light';
    button.innerHTML=icon(next==='light'?'moon':'sun');
    button.setAttribute('aria-label',`Switch to ${target} mode`);
    button.title=`Switch to ${target} mode`;
  });
}
function avatar(profile, large = false) {
  const url = assetUrl(profile?.profileImageUrl);
  return `<span class="avatar${large ? ' avatar-lg' : ''}">${url ? `<img src="${e(url)}" alt="${e(profile?.userName || 'Your')} profile photo">` : e((profile?.userName || 'Q').slice(0, 1).toUpperCase())}</span>`;
}
const hasRole = role => state.profile?.roles?.includes(role);
const isStaff = () => hasRole('Lecturer') || hasRole('Admin');
const isStudent = () => hasRole('Student');
const currentRole = () => (state.profile?.roles || []).join(' · ') || 'Member';
const btn = (label, action, extra = '', style = 'primary') => `<button type="button" class="btn btn-${style}" data-action="${action}" ${extra}>${label}</button>`;
const smallButton = (label, action, extra = '', style = 'secondary') => `<button type="button" class="btn btn-${style} btn-sm" data-action="${action}" ${extra}>${label}</button>`;
const iconButton = (name, label, action, extra = '') => `<button type="button" class="icon-button" title="${e(label)}" aria-label="${e(label)}" data-action="${action}" ${extra}>${icon(name)}</button>`;
const errorNotice = error => `<div class="notice" role="alert">${icon('info')}<div><strong>We couldn’t load this page.</strong><p>${e(error.message)}</p>${smallButton('Try again', 'reload')}</div></div>`;

function setNavigationOpen(open, focus = false) {
  const navigation = document.querySelector('#primary-navigation');
  if (open && !navigation) return;
  document.body.classList.toggle('menu-open', open);
  const backdrop = document.querySelector('.topbar-nav-backdrop');
  if (backdrop) backdrop.hidden = !open;
  const main = document.querySelector('.main-content');
  if (main) main.inert = open;
  const toggle = document.querySelector('.topbar-mobile-toggle');
  if (toggle) {
    toggle.setAttribute('aria-expanded', String(open));
    toggle.setAttribute('aria-label', open ? 'Close navigation' : 'Open navigation');
    toggle.innerHTML = icon(open ? 'close' : 'menu');
  }
  if (focus && open) requestAnimationFrame(() => navigation.querySelector('a')?.focus());
  else if (focus && toggle) toggle.focus();
}
function openModal(title, body) {
  setNavigationOpen(false);
  modalVersion++;
  if (modal.open) modal.close();
  returnFocus = document.activeElement;
  modal.innerHTML = `<div class="modal-heading"><h2 id="modal-title">${e(title)}</h2>${iconButton('close', 'Close dialog', 'close-modal', 'class="modal-close"')}</div><div class="modal-body">${body}</div>`;
  modal.showModal();
  requestAnimationFrame(() => modal.querySelector('input,select,textarea,button')?.focus());
}
modal.addEventListener('close', () => { if (returnFocus?.isConnected) returnFocus.focus(); });
modal.addEventListener('click', event => { if (event.target === modal) { const rect = modal.getBoundingClientRect(); if (event.clientX < rect.left || event.clientX > rect.right || event.clientY < rect.top || event.clientY > rect.bottom) modal.close(); } });
function confirmAction(title, text, label, callback, danger = false) {
  openModal(title, `<p class="subtitle">${e(text)}</p><div class="modal-actions">${btn('Cancel', 'close-modal', '', 'secondary')}<button type="button" class="btn btn-${danger ? 'danger' : 'primary'}" id="confirm-action">${e(label)}</button></div>${formError()}`);
  const version = modalVersion;
  modal.querySelector('#confirm-action').addEventListener('click', async event => {
    busy(event.currentTarget, true, 'Please wait…');
    try { await callback(); if (version === modalVersion && modal.open) modal.close(); } catch (error) { if (version === modalVersion && modal.open) { showFormError(modal, error); busy(modal.querySelector('#confirm-action'), false); } else toast(error.message, 'error'); }
  });
}
function settingsDialog() {
  const settings = getSettings();
  openModal('API connection', `<p class="subtitle">Use the HTTPS address printed when your QuizMaster backend starts.</p><form id="settings-form">${field('API address', 'apiBaseUrl', 'type="url" placeholder="https://localhost:7282"', settings.apiBaseUrl, 'Leave empty only when these files are served by the same ASP.NET app. Do not add /api.')}${formError()}<div class="modal-actions">${btn('Cancel', 'close-modal', '', 'secondary')}<button class="btn btn-primary" type="submit">Save connection</button></div></form>`);
  modal.querySelector('form').addEventListener('submit', async event => {
    event.preventDefault();
    const form = event.currentTarget;
    try {
      const url = form.elements.apiBaseUrl.value.trim().replace(/\/+$/, '');
      if (url && !/^https?:\/\//i.test(url)) throw new Error('Enter a valid http:// or https:// address.');
      setSettings({ apiBaseUrl: url });
      if (!isDemo()) { clearSession(); state.profile = null; }
      modal.close();
      toast('Connection saved.');
      if (!isDemo()) renderAuth('login');
    } catch (error) { showFormError(form, error); }
  });
}

function renderAuth(mode = 'login') {
  state.routeVersion++; state.authVersion++;
  state.profile = null; state.active = null;
  clearTimeout(expiryTimer);
  clearInterval(state.timer); state.timer = null;
  setNavigationOpen(false);
  document.body.classList.remove('layout-topnav', 'sidebar-collapsed');
  const verify = mode === 'verify';
  const register = mode === 'register';
  const title = verify ? 'Check your inbox.' : register ? 'Your next chapter starts here.' : 'Welcome back.';
  document.title = `${verify ? 'Verify email' : register ? 'Create account' : 'Sign in'} · QuizMaster`;
  app.innerHTML = `<main id="main-content" class="auth-layout">
    <section class="auth-brand-panel"><a href="#login" class="brand"><span class="brand-mark">${icon('brand')}</span><span>QuizMaster</span></a><div class="auth-story"><span class="eyebrow">A little curiosity goes a long way</span><h1>Good questions.<br>New possibilities.</h1><p>A quiet space to challenge your knowledge and see how far you can go.</p><div class="auth-illustration" aria-hidden="true">${featureArt()}</div><div class="auth-feature-list"><span>${icon('quiz')} Thoughtful quizzes</span><span>${icon('chart')} Progress that matters</span><span>${icon('trophy')} A little friendly competition</span></div></div><p class="auth-brand-footer">Keep learning. Keep discovering.</p></section>
    <section class="auth-form-panel"><div class="auth-top-actions">${themeButton()}${iconButton('settings', 'API connection settings', 'settings')}</div><div class="auth-card"><span class="eyebrow">${verify ? 'One last step' : 'Make room for curiosity'}</span><h2>${title}</h2><p class="subtitle">${verify ? 'Enter the six-digit code sent to your email.' : register ? 'Create an account to start your first quiz.' : 'Sign in to pick up where you left off.'}</p>
      ${!verify ? `<div class="auth-tabs"><button type="button" data-auth="login" class="auth-tab ${!register ? 'active' : ''}">Sign in</button><button type="button" data-auth="register" class="auth-tab ${register ? 'active' : ''}">Create account</button></div>` : ''}
      <form id="auth-form" data-mode="${mode}">${register ? field('Username', 'userName', 'required maxlength="50" autocomplete="username" placeholder="How should we call you?"') : ''}${field('Email address', 'email', 'type="email" required maxlength="256" autocomplete="email" placeholder="you@example.com"', state.authEmail)}${verify ? field('Verification code', 'code', 'required inputmode="numeric" pattern="[0-9]{6}" maxlength="6" autocomplete="one-time-code" placeholder="000000"') : field('Password', 'password', `type="password" required ${register ? 'minlength="6" maxlength="100" autocomplete="new-password"' : 'autocomplete="current-password"'} placeholder="Enter your password"`, '', register ? 'Use uppercase and lowercase letters, a number, and a symbol.' : '')}${formError()}<button class="btn btn-primary auth-submit" type="submit">${verify ? 'Verify email' : register ? 'Create account' : 'Sign in'} ${icon('arrow')}</button></form>
      <div class="auth-footer">${verify ? '<button class="text-link" data-auth="login">Back to sign in</button>' : '<button class="text-link" data-auth="verify">Already registered? Verify your email</button>'}</div>
      <div class="divider"><span>Take a look first</span></div>${btn('Explore the design demo ' + icon('arrow'), 'demo', '', 'secondary')}<p class="field-help">The demo uses sample data and does not create real accounts.</p>
    </div><p class="auth-bottom-note">Your next challenge is waiting.</p></section></main>`;
  app.querySelector('#auth-form').addEventListener('submit', submitAuth);
}
async function submitAuth(event) {
  event.preventDefault();
  const form = event.currentTarget;
  const button = form.querySelector('button[type="submit"]');
  const dto = Object.fromEntries(new FormData(form));
  const mode = form.dataset.mode;
  busy(button, true, mode === 'login' ? 'Signing in…' : 'Please wait…');
  try {
    if (mode === 'register') {
      await api.register(dto); state.authEmail = dto.email; renderAuth('verify'); toast('Account created. Check your email for the code.');
    } else if (mode === 'verify') {
      await api.verifyEmail(dto); state.authEmail = dto.email; renderAuth('login'); toast('Email verified. You can sign in now.');
    } else {
      const session = await api.login(dto); setSession(session); await boot();
    }
  } catch (error) { showFormError(form, error); busy(button, false); }
}

function renderShell() {
  const links = [['dashboard','home','Dashboard'],['quizzes','quiz','Quizzes'], ...(isStudent() ? [['attempts','attempts','My attempts']] : []), ['leaderboard','chart','Leaderboard'], ...(isStaff() ? [['manage','edit','Manage quizzes']] : []), ...(hasRole('Admin') ? [['admin','users','User roles']] : []), ['profile','user','Profile']];
  document.body.classList.add('layout-topnav');
  document.body.classList.remove('sidebar-collapsed', 'menu-open');
  app.innerHTML = `<div class="app-shell">
    <header class="topbar"><div class="topbar-inner">
      <a class="brand topbar-brand" href="#dashboard"><span class="brand-mark">${icon('brand')}</span><span class="brand-name">QuizMaster</span></a>
      <nav class="topbar-nav" id="primary-navigation" aria-label="Main navigation"><div class="nav-list">${links.map(([route,name,label]) => `<a class="nav-link" href="#${route}" data-route="${route}" title="${label}"><span class="nav-icon">${icon(name)}</span><span class="nav-label">${label}</span></a>`).join('')}</div></nav>
      <div class="topbar-actions">
        ${themeButton()}${iconButton('settings', 'API connection', 'settings')}
        <a class="topbar-user" href="#profile" data-route="profile" aria-label="${e(`Open ${state.profile.userName}’s profile`)}">${avatar(state.profile)}<span class="topbar-user-info user-info"><span class="user-name">${e(state.profile.userName)}</span><span class="user-role">${e(currentRole())}</span></span></a>
        <button class="icon-button logout-button" data-action="logout" type="button" title="Log out" aria-label="Log out">${icon('logout')}</button>
        <button class="icon-button topbar-mobile-toggle" data-action="mobile-menu" type="button" aria-label="Open navigation" aria-controls="primary-navigation" aria-expanded="false">${icon('menu')}</button>
      </div>
    </div></header>
    <button class="topbar-nav-backdrop" data-action="mobile-menu" type="button" aria-label="Close navigation" tabindex="-1" hidden></button>
    <main id="main-content" class="main-content" tabindex="-1">
      ${isDemo() ? `<div class="demo-banner"><span>${icon('info')} <strong>Design demo</strong><span class="demo-description">Sample data. Changes stay in this preview.</span></span><div><label for="demo-role" class="sr-only">Preview as role</label><select id="demo-role" aria-label="Preview as role">${['Student','Lecturer','Admin'].map(role=>`<option ${hasRole(role)?'selected':''}>${role}</option>`).join('')}</select><button class="text-link" data-action="exit-demo">Connect my API ${icon('arrow')}</button></div></div>` : ''}
      <div id="page" class="page-content"></div>
    </main>
  </div>`;
}
function header(title, subtitle, actions = '', eyebrow = '') {
  return `<header class="page-header"><div>${eyebrow ? `<span class="eyebrow">${e(eyebrow)}</span>` : ''}<h1>${e(title)}</h1>${subtitle ? `<p class="subtitle">${e(subtitle)}</p>` : ''}</div><div class="header-actions">${actions}</div></header>`;
}
function featureArt() {
  return `<svg viewBox="0 0 440 210" fill="none" role="presentation"><defs><radialGradient id="glow"><stop stop-color="#a18aeb" stop-opacity=".65"/><stop offset="1" stop-color="#a18aeb" stop-opacity="0"/></radialGradient></defs><ellipse cx="205" cy="105" rx="160" ry="100" fill="url(#glow)" opacity=".25"/><g stroke="currentColor" opacity=".65"><path d="M80 74 101 43 137 34 166 49 181 77 171 103 141 126 133 155"/><circle cx="80" cy="74" r="2.5"/><circle cx="101" cy="43" r="2.5"/><circle cx="137" cy="34" r="2.5"/><circle cx="166" cy="49" r="2.5"/><circle cx="181" cy="77" r="2.5"/><circle cx="171" cy="103" r="2.5"/><circle cx="141" cy="126" r="2.5"/><circle cx="133" cy="155" r="2.5"/><circle cx="133" cy="184" r="3.5" fill="currentColor"/><rect x="242" y="67" width="159" height="27" rx="13.5"/><rect x="242" y="110" width="159" height="27" rx="13.5"/><rect x="242" y="153" width="159" height="27" rx="13.5"/><circle cx="257" cy="80" r="7"/><circle cx="257" cy="123" r="7"/><circle cx="257" cy="166" r="7"/><path d="m373 122 5 5 9-11" stroke-width="2"/></g><g fill="currentColor" opacity=".5"><circle cx="32" cy="53" r="1"/><circle cx="218" cy="31" r="1"/><circle cx="410" cy="40" r="1.3"/><circle cx="61" cy="143" r="1"/><circle cx="216" cy="170" r="1"/><circle cx="431" cy="126" r="1"/></g></svg>`;
}
function stat(label, value, name) { return `<article class="stat-card"><span class="stat-icon">${icon(name)}</span><div class="stat-copy"><span class="stat-label">${e(label)}</span><strong class="stat-value">${e(value)}</strong></div></article>`; }
function latestQuizAttempt(quizId) {
  let latest = null;

  for (const attempt of state.attempts) {
    if (Number(attempt.quizId) !== Number(quizId)) continue;
    if (!latest) {
      latest = attempt;
      continue;
    }

    const currentTime = asUtc(attempt.startedAt).getTime();
    const latestTime = asUtc(latest.startedAt).getTime();
    const currentOrder = Number.isFinite(currentTime) ? currentTime : -Infinity;
    const latestOrder = Number.isFinite(latestTime) ? latestTime : -Infinity;

    if (currentOrder > latestOrder ||
        (currentOrder === latestOrder && Number(attempt.id) > Number(latest.id))) {
      latest = attempt;
    }
  }

  return latest;
}

function quizAttemptStatus(quiz, attempt) {
  if (!attempt) return 'Not started';
  if (completed(attempt)) return 'Submitted';

  const startedAt = asUtc(attempt.startedAt).getTime();
  const timeLimit = Number(quiz.timeLimitMinutes);
  const deadline = startedAt + timeLimit * 60000;

  if (Number.isFinite(deadline) && timeLimit > 0 && Date.now() >= deadline) {
    return 'Time ended';
  }

  return 'In progress';
}

function quizProgress(quiz) {
  if (!isStudent()) return '';

  const attempt = latestQuizAttempt(quiz.id);
  const status = quizAttemptStatus(quiz, attempt);
  const statusClass = status === 'Submitted' ? 'is-submitted' :
    status === 'Time ended' ? 'is-expired' : '';

  if (!attempt) {
    return '<div class="quiz-progress"><span class="quiz-progress-status">Not started</span></div>';
  }

  const hasCounts = typeof attempt.answeredQuestions === 'number' &&
    typeof attempt.totalQuestions === 'number' &&
    Number.isFinite(attempt.answeredQuestions) &&
    Number.isFinite(attempt.totalQuestions);

  if (!hasCounts) {
    return `<div class="quiz-progress ${statusClass}"><div class="quiz-progress-heading"><span class="quiz-progress-status">${status}</span><span class="quiz-progress-count">Progress unavailable</span></div></div>`;
  }

  const total = Math.max(0, Math.trunc(attempt.totalQuestions));
  const answered = Math.min(total, Math.max(0, Math.trunc(attempt.answeredQuestions)));
  const label = `${answered} of ${total} questions answered`;
  const percent = total > 0 ? answered / total * 100 : 0;

  return `<div class="quiz-progress ${statusClass}">
    <div class="quiz-progress-heading">
      <span class="quiz-progress-count">${e(label)}</span>
      <span class="quiz-progress-status">${status}</span>
    </div>
    ${total > 0 ? `<div class="progress-bar" role="progressbar" aria-label="Questions answered in your latest attempt" aria-valuemin="0" aria-valuemax="${total}" aria-valuenow="${answered}" aria-valuetext="${e(label)}. ${status}."><span style="width:${percent}%"></span></div>` : ''}
  </div>`;
}

function studentQuizButton(quiz, compact = false) {
  const attempt = latestQuizAttempt(quiz.id);
  const status = quizAttemptStatus(quiz, attempt);
  const resume = attempt && !completed(attempt);
  const label = status === 'Time ended' ? 'Submit saved answers' :
    resume ? 'Resume quiz' : attempt ? 'Start again' : 'Start quiz';
  const makeButton = compact ? smallButton : btn;

  return makeButton(
    label + (compact ? '' : ' ' + icon('arrow')),
    resume ? 'resume' : 'start',
    `data-id="${resume ? attempt.id : quiz.id}"`,
    'primary'
  );
}

function quizCreator(quiz) {
  const name = typeof quiz?.createdByUserName === 'string' ? quiz.createdByUserName.trim() : '';
  if (!name) return '<p class="quiz-creator quiz-creator-unrecorded">Creator not recorded</p>';
  const initials = name.split(/\s+/).slice(0, 2).map(part => Array.from(part)[0] || '').join('').toUpperCase();
  const url = assetUrl(quiz.createdByProfileImageUrl);

  return `<p class="quiz-creator">
    <span class="creator-avatar" data-creator-initials="${e(initials)}" aria-hidden="true">${url ? `<img src="${e(url)}" alt="" loading="lazy" decoding="async">` : e(initials)}</span>
    <span class="quiz-creator-label">Created by <span class="quiz-creator-name">${e(name)}</span></span>
  </p>`;
}
function quizRow(quiz, management = false) {
  const student = isStudent() && !management;

  return `<div class="quiz-row ${student ? 'quiz-row--progress' : ''}">
    <span class="quiz-icon">${icon(quizIcon(quiz.title))}</span>
    <div class="quiz-info">
      <h3 class="quiz-title">${e(quiz.title)}</h3>
      <span class="quiz-meta">${icon('clock')}${e(quiz.timeLimitMinutes)} min${management ? ` <span class="badge ${quiz.isPublished ? 'badge-success' : 'badge-muted'}">${quiz.isPublished ? 'Published' : 'Draft'}</span>` : ''}</span>
      ${quizCreator(quiz)}
      ${student ? quizProgress(quiz) : ''}
    </div>
    <div class="row-actions">${management ? smallButton('Edit quiz ' + icon('arrow'), 'manage-quiz', `data-id="${quiz.id}"`) : student ? studentQuizButton(quiz, true) : smallButton('View leaderboard', 'board', `data-id="${quiz.id}"`)}</div>
  </div>`;
}

function attemptRow(attempt) {
  const quiz = state.quizzes.find(q => q.id === attempt.quizId);
  const done = completed(attempt);
  const expired = !done && quiz && Date.now() >= asUtc(attempt.startedAt).getTime() + quiz.timeLimitMinutes * 60000;
  return `<div class="attempt-row"><span class="quiz-icon">${icon(quizIcon(quiz?.title || ''))}</span><div class="quiz-info"><h3 class="quiz-title">${e(quiz?.title || `Quiz #${attempt.quizId}`)}</h3><span class="quiz-meta">${dateLabel(attempt.startedAt)}</span></div><span class="badge ${done ? 'badge-success' : expired ? 'badge-muted' : 'badge-info'}">${done ? 'Completed' : expired ? 'Time ended' : 'In progress'}</span><div class="row-actions">${!done ? iconButton('chevron', expired ? 'Submit saved answers' : 'Resume quiz', 'resume', `data-id="${attempt.id}"`) : `<span class="attempt-completed-mark" title="Completed">${icon('check')}</span>`}</div></div>`;
}
function boardPanel(quizzes, full = false, selected) {
  const selector = `<div class="field"><label for="board-quiz" class="${full ? '' : 'sr-only'}">Choose a quiz</label><select id="board-quiz">${quizzes.length ? quizzes.map(quiz=>`<option value="${e(quiz.id)}" ${Number(selected)===quiz.id?'selected':''}>${e(quiz.title)}</option>`).join('') : '<option value="">No quizzes available</option>'}</select></div>`;
  if (!full) return `<div class="panel leaderboard-panel">${selector}<div id="board-rows" aria-live="polite"><div class="loading-state">Loading standings…</div></div></div>`;

  return `<div class="panel leaderboard-picker">${selector}</div>
    <div class="leaderboard-layout">
      <section class="panel leaderboard-panel" aria-labelledby="standings-title">
        <div class="section-header"><h2 id="standings-title">Standings</h2>${icon('trophy')}</div>
        <div id="board-rows" aria-live="polite"><div class="loading-state">Loading standings…</div></div>
      </section>
      <section class="panel leaderboard-chart-panel" aria-labelledby="leaderboard-chart-title">
        <div class="section-header"><h2 id="leaderboard-chart-title">Top five scores</h2>${icon('chart')}</div>
        <div id="board-chart" aria-live="polite"><div class="loading-state">Loading score comparison…</div></div>
      </section>
    </div>`;
}
function leaderboardChart(rows) {
  const leaders = rows.slice(0, 5);
  const hasScale = leaders.some(row => typeof row.maxScore === 'number' && Number.isFinite(row.maxScore) && row.maxScore > 0);
  const items = leaders.map(row => {
    const validScore = typeof row.score === 'number' && Number.isFinite(row.score);
    const validMaximum = typeof row.maxScore === 'number' && Number.isFinite(row.maxScore);
    const hasPoints = validScore && validMaximum && row.maxScore > 0;
    const ratio = hasPoints ? row.score / row.maxScore * 100 : NaN;
    const height = Number.isFinite(ratio) ? Math.max(0, Math.min(100, ratio)) : 0;
    const withinRange = hasPoints && row.score >= 0 && row.score <= row.maxScore && Number.isFinite(ratio);
    const fraction = `${validScore ? row.score : '—'} / ${validMaximum ? row.maxScore : '—'}`;
    const description = validMaximum && row.maxScore === 0 ? 'No available points' : withinRange ? `${Math.round(height)}%` : 'Percentage unavailable';

    return `<li class="leaderboard-column" style="--score-height:${height}%">
      <div class="leaderboard-column-label"><span class="leaderboard-column-rank">${e(row.rank)}.</span><span class="leaderboard-column-name">${e(row.userName)}</span></div>
      <div class="leaderboard-column-plot">
        <span class="leaderboard-column-score">${e(fraction)}<span class="sr-only"> points</span></span>
        <span class="leaderboard-column-fill" aria-hidden="true"></span>
      </div>
      <p class="leaderboard-column-percentage">${e(description)}${withinRange ? '<span class="sr-only"> of available points</span>' : ''}</p>
    </li>`;
  }).join('');

  const ticks = [100, 75, 50, 25, 0];
  return `<div class="leaderboard-column-chart">
    <div class="leaderboard-column-axis" aria-hidden="true">${hasScale ? ticks.map((tick, index) => `<span style="top:${index * 25}%">${tick}%</span>`).join('') : ''}</div>
    <div class="leaderboard-column-body">
      <div class="leaderboard-column-grid" aria-hidden="true">${ticks.map((tick, index) => `<span style="top:${index * 25}%"></span>`).join('')}</div>
      <ol class="leaderboard-column-list" aria-label="Top five participants and scores" style="--column-count:${leaders.length}">${items}</ol>
    </div>
  </div>`;
}
async function loadBoard(id) {
  const target = document.querySelector('#board-rows');
  if (!target) return;
  const chart = document.querySelector('#board-chart');
  const version = ++state.leaderboardVersion;
  if (!id) {
    target.innerHTML = empty('No standings yet', 'Choose a quiz to see the leaderboard.');
    if (chart?.isConnected) chart.innerHTML = empty('Choose a quiz', 'Its top scores will appear here.');
    return;
  }
  target.innerHTML = '<div class="loading-state" role="status">Loading standings…</div>';
  if (chart?.isConnected) chart.innerHTML = '<div class="loading-state" role="status">Loading score comparison…</div>';
  try {
    const rows = await api.leaderboard(id);
    if (version !== state.leaderboardVersion || !target.isConnected) return;
    if (!Array.isArray(rows)) throw new Error('The leaderboard response could not be read.');
    target.innerHTML = rows.length ? rows.map(row=>`<div class="leaderboard-row"><span class="rank ${row.rank===1?'rank-first':''}">${e(row.rank)}.</span><span class="leader-name">${e(row.userName)}</span><span class="score">${e(row.score)}<span class="muted">/${e(row.maxScore)}</span></span></div>`).join('') : empty('Be the first on the board', 'Complete this quiz to earn a place.');
    if (chart?.isConnected) chart.innerHTML = rows.length ? leaderboardChart(rows) : empty('No scores to compare yet', 'Completed quiz results will appear here.');
  } catch (error) {
    if (version===state.leaderboardVersion && target.isConnected) {
      target.innerHTML = `<p class="error-text" role="alert">${e(error.message)}</p>`;
      if (chart?.isConnected) chart.innerHTML = `<p class="error-text">${e(error.message)}</p>`;
    }
  }
}

async function refreshData(version = state.routeVersion) {
  const [quizzes, attempts] = await Promise.all([isStaff() ? api.allQuizzes() : api.publishedQuizzes(), isStudent() ? api.myAttempts() : Promise.resolve([])]);
  if (version !== state.routeVersion || !state.profile) return;
  state.quizzes = quizzes;
  state.attempts = attempts;
}
function dashboard() {
  const published = state.quizzes.filter(q => q.isPublished);
  const stats = isStudent() ? [stat('Published quizzes', published.length, 'stack'), stat('Your attempts', state.attempts.length, 'clipboard'), stat('Completed', state.attempts.filter(completed).length, 'attempts')] : [stat('All quizzes', state.quizzes.length, 'stack'), stat('Published', published.length, 'attempts'), stat('Drafts', state.quizzes.length-published.length, 'edit')];
  const recent = [...state.attempts].sort((a,b) => asUtc(b.startedAt) - asUtc(a.startedAt) || b.id - a.id).slice(0,3);
  const cards = published.slice(0,3).map(quiz => `<article class="quiz-card dashboard-quiz-card">
    <div class="quiz-card-top"><span class="quiz-icon">${icon(quizIcon(quiz.title))}</span><span class="badge badge-success">Published</span></div>
    <h2>${e(quiz.title)}</h2>
    ${quizCreator(quiz)}
    <p class="quiz-meta">${icon('clock')}${e(quiz.timeLimitMinutes)} minutes</p>
    ${isStudent() ? quizProgress(quiz) : ''}
    <div class="quiz-card-footer">${isStudent() ? studentQuizButton(quiz) : btn('View quiz ' + icon('arrow'), 'manage-quiz', `data-id="${quiz.id}"`)}</div>
  </article>`).join('');

  document.querySelector('#page').innerHTML = `<section class="dashboard-hero" aria-labelledby="dashboard-heading">
    <div class="dashboard-hero-copy"><p class="eyebrow dashboard-welcome">Welcome back, ${e(state.profile.userName)}</p><h1 class="dashboard-heading" id="dashboard-heading">A little curiosity.<br>A world to discover.</h1><p class="subtitle">${isStudent() ? 'Choose a quiz and see where your knowledge takes you.' : 'Create a thoughtful challenge and give curious minds somewhere to start.'}</p><a class="btn btn-primary" href="#quizzes">Explore quizzes ${icon('arrow')}</a></div>
    <div class="dashboard-hero-stats" aria-label="Your overview">${stats.join('')}</div>
  </section>
  <div class="dashboard-discovery">
    <section class="dashboard-library"><div class="section-header"><h2>Choose your next quiz</h2><a class="text-link" href="#quizzes">View all ${icon('arrow')}</a></div><div class="dashboard-quiz-grid">${cards || empty('Your next challenge is on its way', isStaff() ? 'Create a quiz, add questions, and publish it when it’s ready.' : 'Published quizzes will appear here.', isStaff() ? btn('Create a quiz ' + icon('plus'), 'new-quiz') : '')}</div></section>
    <aside class="dashboard-board"><div class="section-header"><h2>Leaderboard</h2><a class="text-link" href="#leaderboard">View all ${icon('arrow')}</a></div>${boardPanel(published)}</aside>
  </div>
  ${isStudent() ? `<section class="dashboard-recent"><div class="section-header"><h2>Recent attempts</h2><a class="text-link" href="#attempts">View all ${icon('arrow')}</a></div><div class="panel dashboard-recent-list">${recent.map(attemptRow).join('') || empty('A fresh start', 'Your quiz attempts will appear here.')}</div></section>` : `<section class="dashboard-recent"><div class="panel workspace-note"><span class="quiz-icon">${icon('edit')}</span><div><h3>Make something worth learning.</h3><p class="subtitle">Build questions, choose the right answers, and give your learners a new challenge.</p><a class="text-link" href="#manage">Manage your quizzes ${icon('arrow')}</a></div></div></section>`}`;
  loadBoard(published[0]?.id);
}
function quizzesPage() {
  const quizzes = state.quizzes.filter(q => q.isPublished);

  document.querySelector('#page').innerHTML = `${header('Find your next challenge.', 'A new question. A different perspective. Start somewhere.', '', 'Quiz library')}
    <div class="toolbar">
      <div class="search-field">${icon('search')}<label for="quiz-search" class="sr-only">Search quizzes</label><input id="quiz-search" type="search" placeholder="Search quizzes…" autocomplete="off"></div>
      <span class="muted" id="quiz-count">${quizzes.length} ${quizzes.length === 1 ? 'quiz' : 'quizzes'}</span>
    </div>
    <div id="quiz-cards" class="quiz-card-grid"></div>`;

  const render = query => {
    const matches = quizzes.filter(q => q.title.toLowerCase().includes(query.toLowerCase()));
    document.querySelector('#quiz-count').textContent = `${matches.length} ${matches.length === 1 ? 'quiz' : 'quizzes'}`;
    document.querySelector('#quiz-cards').innerHTML = matches.map(q => `
      <article class="quiz-card">
        <div class="quiz-card-top"><span class="quiz-icon">${icon(quizIcon(q.title))}</span><span class="badge badge-success">Published</span></div>
        <h2>${e(q.title)}</h2>
        ${quizCreator(q)}
        <p class="quiz-meta">${icon('clock')}${e(q.timeLimitMinutes)} minutes</p>
        ${isStudent() ? quizProgress(q) : ''}
        <div class="quiz-card-footer">${isStudent() ? studentQuizButton(q) : btn('Leaderboard ' + icon('arrow'), 'board', `data-id="${q.id}"`)}</div>
      </article>`).join('') || empty(query ? 'No matches found' : 'Nothing published just yet', query ? 'Try a different title.' : 'Check back when a new quiz is ready.');
  };

  render('');
  document.querySelector('#quiz-search').addEventListener('input', event => render(event.target.value));
}

function attemptsPage() {
  document.querySelector('#page').innerHTML = `${header('Every attempt counts.', 'Pick up where you left off, or look back at what you’ve completed.', '', 'Your journey')}<div class="toolbar"><div class="filter-tabs" aria-label="Filter attempts">${[['all','All attempts'],['active','In progress'],['completed','Completed']].map(([value,label])=>`<button class="filter-tab ${state.filter===value?'active':''}" data-filter="${value}">${label}</button>`).join('')}</div></div><div class="panel" id="attempt-list"></div><p class="field-help">Your history shows attempt status. Scores are displayed when you finish a quiz and on its leaderboard.</p>`;
  drawAttempts();
}
function drawAttempts() {
  const matches = [...state.attempts].filter(a=>state.filter==='all'||(state.filter==='completed'?completed(a):!completed(a))).sort((a,b)=>asUtc(b.startedAt)-asUtc(a.startedAt));
  document.querySelector('#attempt-list').innerHTML = matches.map(attemptRow).join('') || empty('Your next step is a quiz away', 'Choose a published quiz to get started.', '<a class="btn btn-primary" href="#quizzes">Explore quizzes</a>');
}
function leaderboardPage(selected) {
  const quizzes = state.quizzes.filter(q=>q.isPublished || isStaff());
  document.querySelector('#page').innerHTML = `${header('A little friendly competition.', 'Your best result earns your place. Every quiz is a new opportunity.', '', 'Leaderboard')}<section class="leaderboard-full">${boardPanel(quizzes,true,selected)}</section>`;
  loadBoard(Number(document.querySelector('#board-quiz').value));
}

async function showRoute() {
  if (!state.profile) return;
  const version = ++state.routeVersion;
  let [route, id] = location.hash.slice(1).split('/');
  route = route || 'dashboard';
  if (['login','register','verify'].includes(route)) route='dashboard';
  if ((['manage','edit','statistics'].includes(route) && !isStaff()) || (route==='admin'&&!hasRole('Admin')) || (['attempt','attempts','result'].includes(route)&&!isStudent())) { location.hash = 'dashboard'; return; }
  state.route = route;
  clearInterval(state.timer); state.timer = null;
  setNavigationOpen(false);
  document.querySelectorAll('[data-route]').forEach(link=>{ const active=link.dataset.route===(['edit','statistics'].includes(route)?'manage':route==='attempt'||route==='result'?'attempts':route); if(active)link.setAttribute('aria-current','page');else link.removeAttribute('aria-current'); });
  document.querySelector('#page').innerHTML = '<div class="loading-state" role="status">Getting things ready…</div>';
  document.title = `${({dashboard:'Dashboard',quizzes:'Quizzes',attempts:'My attempts',leaderboard:'Leaderboard',manage:'Manage quizzes',edit:'Quiz editor',profile:'Profile',admin:'User roles',attempt:'Quiz in progress',statistics:'Statistics',result:'Your result'}[route]||'Dashboard')} · QuizMaster`;
  try {
    if (route==='profile') { const profile=await api.profile(); if(version===state.routeVersion && state.profile) { state.profile=profile; profilePage(); } }
    else if(route==='admin') adminPage();
    else if(route==='edit') await editorPage(Number(id), version);
    else if(route==='statistics') await statisticsPage(Number(id), version);
    else if(route==='attempt') await attemptPage(Number(id), version);
    else if(route==='result') resultPage();
    else {
      await refreshData(version); if(version!==state.routeVersion)return;
      if(route==='quizzes')quizzesPage();else if(route==='attempts')attemptsPage();else if(route==='leaderboard')leaderboardPage(id);else if(route==='manage')managePage();else dashboard();
    }
    if(version===state.routeVersion) window.scrollTo({top:0,behavior:'instant'});
  } catch(error) { if(version===state.routeVersion && state.profile) document.querySelector('#page').innerHTML=errorNotice(error); }
}

function managePage() {
  document.querySelector('#page').innerHTML = `${header('Ideas become quizzes.', 'Create, refine, and share a new challenge.', btn(icon('plus')+' Create quiz','new-quiz'), 'Your workspace')}<div class="toolbar"><div class="search-field">${icon('search')}<label class="sr-only" for="manage-search">Search quizzes</label><input id="manage-search" type="search" placeholder="Find a quiz…"></div></div><div class="panel" id="manage-list">${state.quizzes.map(q=>quizRow(q,true)).join('')||empty('Start with a good question','Create your first quiz to begin.')}</div>`;
  document.querySelector('#manage-search').addEventListener('input',event=>{const matches=state.quizzes.filter(q=>q.title.toLowerCase().includes(event.target.value.toLowerCase()));document.querySelector('#manage-list').innerHTML=matches.map(q=>quizRow(q,true)).join('')||empty('No matching quizzes');});
}
function quizDialog(quiz) {
  openModal(quiz ? 'Edit quiz details' : 'Create a quiz', `<form id="quiz-form">${field('Quiz title','title','required maxlength="100" placeholder="Give your quiz a title"',quiz?.title)}${field('Time limit (minutes)','timeLimitMinutes','type="number" required min="1" max="2147483647" step="1"',quiz?.timeLimitMinutes||20)}${formError()}<div class="modal-actions">${btn('Cancel','close-modal','','secondary')}<button class="btn btn-primary" type="submit">${quiz?'Save changes':'Create quiz'}</button></div></form>`);
  modal.querySelector('form').addEventListener('submit',async event=>{event.preventDefault(); const form=event.currentTarget;const button=form.querySelector('[type=submit]');busy(button,true);try{const dto={title:form.elements.title.value.trim(),timeLimitMinutes:Number(form.elements.timeLimitMinutes.value)};const result=quiz?await api.updateQuiz(quiz.id,dto):await api.createQuiz(dto);modal.close();toast(quiz?'Quiz updated.':'Your quiz is ready for questions.');if(location.hash===`#edit/${result.id}`)showRoute();else location.hash=`edit/${result.id}`;}catch(error){showFormError(form,error);busy(button,false);}});
}
function questionTypeValue(question) {
  const type = question?.type;
  return ({ SingleChoice: 0, MultipleChoice: 1, TrueFalse: 2 })[type] ?? Number(type ?? 0);
}
function isTrueFalse(question) { return questionTypeValue(question) === 2; }
function trueFalseCorrectAnswer(question) {
  const options = question?.options || [];
  if (options.length !== 2) return null;
  const trueOption = options.find(option => option.text === 'True');
  const falseOption = options.find(option => option.text === 'False');
  if (!trueOption || !falseOption || typeof trueOption.isCorrect !== 'boolean' ||
      typeof falseOption.isCorrect !== 'boolean' || trueOption.isCorrect === falseOption.isCorrect) return null;
  return trueOption.isCorrect;
}
function editorQuestionCard(question, index, quiz) {
  const fixed = isTrueFalse(question);
  const correctAnswer = fixed ? trueFalseCorrectAnswer(question) : null;
  const repair = fixed && correctAnswer === null;
  const options = fixed && !repair
    ? ['True', 'False'].map(text => question.options.find(option => option.text === text))
    : question.options;
  const disabled = quiz.isPublished ? 'disabled' : '';

  return `<article class="question-card ${fixed ? 'true-false-card' : ''}">
    <div class="question-card-heading">
      <span class="question-number">${index + 1}</span>
      <div><span class="eyebrow">${typeLabel(question.type)} · ${question.points} ${question.points === 1 ? 'point' : 'points'}</span><h3>${e(question.text)}</h3></div>
      <div class="row-actions">${iconButton('edit', fixed ? 'Edit question and correct answer' : 'Edit question', 'edit-question', `data-id="${question.id}" ${disabled}`)}${iconButton('trash', 'Delete question', 'delete-question', `data-id="${question.id}" ${disabled}`)}</div>
    </div>
    ${repair ? `<div class="notice true-false-repair">${icon('info')}<p>This true or false question needs a valid answer pair. Edit the question and choose its correct answer to replace these options with True and False. The quiz must be editable.</p></div>` : ''}
    <div class="option-list">${options.map(option => `<div class="option-row">
      <span class="option-marker ${option.isCorrect ? 'correct' : ''}">${option.isCorrect ? icon('check') : ''}</span><span>${e(option.text)}</span>
      ${option.isCorrect ? '<span class="correct-label">Correct</span>' : ''}
      ${fixed ? '' : `<div class="row-actions">${iconButton('edit', 'Edit answer option', 'edit-option', `data-id="${option.id}" data-question="${question.id}" ${disabled}`)}${iconButton('trash', 'Delete answer option', 'delete-option', `data-id="${option.id}" ${disabled}`)}</div>`}
    </div>`).join('') || `<p class="field-help">${fixed ? 'Edit this question to create its True and False options.' : 'Add answer options and mark the correct answer.'}</p>`}</div>
    ${fixed ? '<p class="field-help true-false-options-note">True and False are managed together. Use Edit question to change the correct answer.</p>' : smallButton(icon('plus') + ' Add answer option', 'new-option', `data-id="${question.id}" ${disabled}`)}
  </article>`;
}
async function editorPage(id, version) {
  if (!id) { location.hash = 'manage'; return; }
  const [quiz, questions] = await Promise.all([api.quiz(id), api.questions(id)]);
  const optionResults = await Promise.all(questions.map(async question => ({ ...question, options: await api.options(question.id) })));
  if (version !== state.routeVersion) return;
  state.editor = { quiz, questions: optionResults };
  document.querySelector('#page').innerHTML = `<a class="text-link back-link" href="#manage">${icon('left')} All quizzes</a>${header(quiz.title, `${quiz.timeLimitMinutes} minute time limit · ${questions.length} ${questions.length === 1 ? 'question' : 'questions'}`, `<span class="badge ${quiz.isPublished ? 'badge-success' : 'badge-muted'}">${quiz.isPublished ? 'Published' : 'Draft'}</span>${btn(quiz.isPublished ? 'Unpublish' : 'Publish quiz', quiz.isPublished ? 'unpublish' : 'publish', `data-id="${id}"`)}${iconButton('edit', 'Edit quiz details', 'edit-quiz', `data-id="${id}"`)}${iconButton('trash', 'Delete quiz', 'delete-quiz', `data-id="${id}"`)}`, 'Quiz editor')}
    <div class="quiz-editor-creator">${quizCreator(quiz)}</div>
    ${quiz.isPublished ? `<div class="notice">${icon('lock')}<div><strong>This quiz is published.</strong><p>Unpublish it before changing questions. Quizzes with attempt history stay protected.</p></div></div>` : ''}
    <div class="section-header"><h2>Questions</h2><div class="row-actions"><a class="text-link" href="#statistics/${id}">${icon('chart')} Statistics</a>${btn(icon('plus') + ' Add question', 'new-question', `data-id="${id}" ${quiz.isPublished ? 'disabled' : ''}`, 'secondary')}</div></div>
    <div class="question-list">${optionResults.map((question, index) => editorQuestionCard(question, index, quiz)).join('') || empty('Give your quiz its first question', 'Choose a question type, add options, and mark what’s correct.', btn(icon('plus') + ' Add question', 'new-question', `data-id="${id}" ${quiz.isPublished ? 'disabled' : ''}`))}</div>`;
}
function questionDialog(quizId, question) {
  const initialType = question ? questionTypeValue(question) : 0;
  const currentAnswer = question && isTrueFalse(question) ? trueFalseCorrectAnswer(question) : null;
  const needsRepair = question && isTrueFalse(question) && currentAnswer === null;

  openModal(question ? 'Edit question' : 'Add a question', `<form id="question-form">
    <div class="field"><label for="text">Question</label><textarea id="text" name="text" required maxlength="1000" rows="3" placeholder="What would you like to ask?">${e(question?.text)}</textarea></div>
    <div class="form-grid">${field('Points', 'points', 'type="number" required min="1" max="2147483647" step="1"', question?.points || 1)}
      <div class="field"><label for="type">Question type</label><select id="type" name="type">${['Single choice', 'Multiple choice', 'True or false'].map((label, index) => `<option value="${index}" ${initialType === index ? 'selected' : ''}>${label}</option>`).join('')}</select></div>
    </div>
    <div class="field true-false-answer-field" id="true-false-answer-field" ${initialType === 2 ? '' : 'hidden'}>
      <label for="trueFalseAnswer">Correct answer</label>
      <select id="trueFalseAnswer" name="trueFalseAnswer" ${initialType === 2 ? 'required' : 'disabled'} aria-describedby="true-false-answer-help">
        <option value="" ${currentAnswer === null ? 'selected' : ''}>Choose the correct answer</option>
        <option value="true" ${currentAnswer === true ? 'selected' : ''}>True</option>
        <option value="false" ${currentAnswer === false ? 'selected' : ''}>False</option>
      </select>
      <p class="field-help" id="true-false-answer-help">True and False options are created automatically. Choose whether the statement is true or false.</p>
      ${needsRepair ? '<p class="field-help true-false-repair-help">Choose an answer to repair this question’s existing options when you save.</p>' : ''}
    </div>
    ${question && !isTrueFalse(question) ? `<div class="notice true-false-conversion" id="true-false-conversion" hidden>${icon('info')}<p>Changing this question to True or false will replace its existing answer options with True and False.</p></div>` : ''}
    ${formError()}<div class="modal-actions">${btn('Cancel', 'close-modal', '', 'secondary')}<button class="btn btn-primary" type="submit">Save question</button></div>
  </form>`);

  const form = modal.querySelector('#question-form');
  const syncType = () => {
    const fixed = Number(form.elements.type.value) === 2;
    form.querySelector('#true-false-answer-field').hidden = !fixed;
    form.elements.trueFalseAnswer.disabled = !fixed;
    form.elements.trueFalseAnswer.required = fixed;
    const conversion = form.querySelector('#true-false-conversion');
    if (conversion) conversion.hidden = !fixed;
  };
  form.elements.type.addEventListener('change', syncType);
  syncType();
  form.addEventListener('submit', async event => {
    event.preventDefault();
    const button = form.querySelector('[type=submit]');
    const dto = { text: form.elements.text.value.trim(), points: Number(form.elements.points.value), type: Number(form.elements.type.value) };
    if (dto.type === 2) {
      const selected = form.elements.trueFalseAnswer.value;
      if (!['true', 'false'].includes(selected)) { showFormError(form, new Error('Choose whether True or False is the correct answer.')); return; }
      dto.trueFalseAnswer = selected === 'true';
    }
    busy(button, true);
    try {
      if (question) await api.updateQuestion(question.id, dto); else await api.addQuestion(quizId, dto);
      modal.close(); toast('Question saved.'); await showRoute();
    } catch (error) { showFormError(form, error); busy(button, false); }
  });
}
function optionDialog(questionId, option) {
  openModal(option?'Edit answer option':'Add an answer option',`<form id="option-form"><div class="field"><label for="text">Answer text</label><textarea id="text" name="text" required maxlength="500" rows="3">${e(option?.text)}</textarea></div><label class="checkbox-label"><input type="checkbox" name="isCorrect" ${option?.isCorrect?'checked':''}> This is a correct answer</label>${formError()}<div class="modal-actions">${btn('Cancel','close-modal','','secondary')}<button class="btn btn-primary" type="submit">Save answer option</button></div></form>`);
  modal.querySelector('form').addEventListener('submit',async event=>{event.preventDefault();const form=event.currentTarget,button=form.querySelector('[type=submit]');busy(button,true);try{const dto={text:form.elements.text.value.trim(),isCorrect:form.elements.isCorrect.checked};if(option)await api.updateOption(option.id,dto);else await api.addOption(questionId,dto);modal.close();toast('Answer option saved.');await showRoute();}catch(error){showFormError(form,error);busy(button,false);}});
}
async function statisticsPage(id,version) {
  const [quiz,stats]=await Promise.all([api.quiz(id),api.statistics(id)]);if(version!==state.routeVersion)return;
  document.querySelector('#page').innerHTML=`<a class="text-link back-link" href="#edit/${id}">${icon('left')} Back to quiz</a>${header(quiz.title,'A closer look at completed attempts.','','Quiz statistics')}<div class="stats-grid">${stat('Completed attempts',stats.attemptCount,'clipboard')}${stat('Average score',Number(stats.averageScore).toLocaleString(undefined,{maximumFractionDigits:2}),'chart')}${stat('Highest score',stats.highestScore,'trophy')}</div><div class="panel statistics-summary"><h2>The full picture</h2><dl class="details-list"><div><dt>Lowest score</dt><dd>${e(stats.lowestScore)}</dd></div><div><dt>Time limit</dt><dd>${e(quiz.timeLimitMinutes)} minutes</dd></div><div><dt>Publication</dt><dd>${quiz.isPublished?'Published':'Draft'}</dd></div></dl></div>`;
}

function adminPage() {
  document.querySelector('#page').innerHTML=`${header('The right role for everyone.','Manage what each person can do in your workspace.','','Administration')}<section class="panel role-card"><div class="section-header"><h2>Change a user’s role</h2>${icon('users')}</div><p class="subtitle">Enter their user ID and select the role to assign. This replaces their current role.</p><form id="role-form">${field('User ID','userId','required placeholder="Paste the user’s ID"')}<div class="field"><label for="roleName">New role</label><select id="roleName" name="roleName"><option>Student</option><option>Lecturer</option><option>Admin</option></select></div><div class="notice">${icon('info')}<p>The user needs to sign in again for their new role to appear in their access token.</p></div>${formError()}<div class="form-actions"><button class="btn btn-primary" type="submit">Update role ${icon('arrow')}</button></div></form></section>`;
  document.querySelector('#role-form').addEventListener('submit',event=>{event.preventDefault();const form=event.currentTarget;const userId=form.elements.userId.value.trim(),roleName=form.elements.roleName.value;confirmAction('Update this user’s role?',`Assign ${roleName} to user ${userId}?`,'Update role',async()=>{await api.changeRole(userId,{roleName});toast('Role updated. The user must sign in again.');});});
}

function profilePage() {
  const p=state.profile;
  const authVersion=state.authVersion;
  const viewVersion=state.routeVersion;
  document.querySelector('#page').innerHTML=`${header('A space that’s yours.','A few details, a familiar face, and you’re ready.','','Your profile')}<div class="profile-grid"><section class="panel profile-card"><div id="profile-avatar">${avatar(p,true)}</div><h2>${e(p.userName)}</h2><p class="subtitle">${e(p.email)}</p><div class="profile-roles">${(p.roles||[]).map(r=>`<span class="badge badge-info">${e(r)}</span>`).join('')}</div><form id="photo-form"><div class="field"><label class="btn btn-secondary" for="photo-file">${icon('upload')} Choose photo</label><input class="sr-only" id="photo-file" name="file" type="file" accept="image/jpeg,image/png"><p class="field-help">JPG or PNG, up to 2 MB.</p></div><p id="photo-selection" class="field-help" hidden></p><button class="btn btn-primary" type="submit" hidden>Upload photo</button>${p.profileImageUrl?smallButton('Remove photo','remove-photo','','secondary'):''}${formError()}</form></section><div class="stack"><section class="panel"><div class="section-header"><h2>Personal details</h2></div><form id="profile-form">${field('Username','userName','required maxlength="50" autocomplete="username"',p.userName)}${field('Email address','profileEmail','type="email" readonly',p.email,'Your email address stays linked to your verified account.')}${field('Your user ID','profileUserId','readonly',getSession()?.userId||'')}${formError()}<div class="form-actions"><button class="btn btn-primary" type="submit">Save changes</button></div></form></section><section class="panel"><div class="section-header"><h2>Change password</h2>${icon('lock')}</div><form id="password-form">${field('Current password','currentPassword','type="password" required autocomplete="current-password"')}${field('New password','newPassword','type="password" required minlength="6" maxlength="100" autocomplete="new-password"')}${field('Confirm new password','confirmPassword','type="password" required autocomplete="new-password"')}${formError()}<div class="form-actions"><button class="btn btn-secondary" type="submit">Update password</button></div></form></section></div></div>`;
  document.querySelector('#profile-form').addEventListener('submit',async event=>{event.preventDefault();const form=event.currentTarget,button=form.querySelector('[type=submit]');busy(button,true);try{const profile=await api.updateProfile({userName:form.elements.userName.value.trim()});if(authVersion!==state.authVersion||!state.profile)return;state.profile=profile;if(viewVersion===state.routeVersion){renderShell();profilePage();}toast('Profile updated.');}catch(error){showFormError(form,error);busy(button,false);}});
  document.querySelector('#password-form').addEventListener('submit',async event=>{event.preventDefault();const form=event.currentTarget,button=form.querySelector('[type=submit]');if(form.elements.newPassword.value!==form.elements.confirmPassword.value){showFormError(form,new Error('Your new passwords don’t match.'));return;}busy(button,true);try{await api.changePassword({currentPassword:form.elements.currentPassword.value,newPassword:form.elements.newPassword.value});form.reset();toast('Password changed.');busy(button,false);}catch(error){showFormError(form,error);busy(button,false);}});
  document.querySelector('#photo-file').addEventListener('change',event=>{
    const form=document.querySelector('#photo-form');const file=event.target.files[0];const submit=form.querySelector('[type=submit]');submit.hidden=true;form.querySelector('.form-error').hidden=true;if(!file)return;
    if(file.size>2*1024*1024 || !['image/jpeg','image/png'].includes(file.type)){event.target.value='';showFormError(form,new Error('Choose a JPG or PNG image smaller than 2 MB.'));return;}
    if(photoPreview)URL.revokeObjectURL(photoPreview);photoPreview=URL.createObjectURL(file);document.querySelector('#profile-avatar').innerHTML=`<span class="avatar avatar-lg"><img src="${e(photoPreview)}" alt="Selected photo preview"></span>`;document.querySelector('#photo-selection').textContent=`${file.name} · Preview only until uploaded`;document.querySelector('#photo-selection').hidden=false;submit.hidden=false;
  });
  document.querySelector('#photo-form').addEventListener('submit',async event=>{event.preventDefault();const form=event.currentTarget,file=form.elements.file.files[0],button=form.querySelector('[type=submit]');if(!file)return;busy(button,true,'Uploading…');try{const profile=await api.uploadPhoto(file);if(authVersion!==state.authVersion||!state.profile)return;state.profile=profile;if(viewVersion===state.routeVersion){renderShell();profilePage();}if(photoPreview){URL.revokeObjectURL(photoPreview);photoPreview=null;}toast('Profile photo updated.');}catch(error){showFormError(form,photoEndpointError(error));busy(button,false);}});
}
function photoEndpointError(error) {return error.status===404?new Error('Photo uploads aren’t available on this server yet. Please try again after profile photos have been enabled.'):error;}

// This cache contains only selections confirmed as saved by the API. The API
// remains the source of truth; a browser refresh never submits empty answers.
function answerCacheKey(id) {return `quizmaster.answers.${isDemo()?'demo':getSession()?.userId}.${id}`;}
function readAnswers(id) {try{return JSON.parse(sessionStorage.getItem(answerCacheKey(id))||'{}');}catch{return {};}}
function writeAnswers(active) {try{sessionStorage.setItem(answerCacheKey(active.attempt.id),JSON.stringify(active.saved));}catch{}}
function startDialog(id) {
  const quiz=state.quizzes.find(q=>q.id===id);
  if(!quiz)return;
  confirmAction(`Ready for ${quiz.title}?`,`You’ll have ${quiz.timeLimitMinutes} minutes. The timer starts as soon as you begin and continues if you leave the page. Save your answers as you go.`,'Start quiz',async()=>{
    const attempt=await api.startAttempt(id);
    state.attempts.push(attempt);state.active=null;location.hash=`attempt/${attempt.id}`;
  });
}
async function attemptPage(id,version) {
  await refreshData(version);if(version!==state.routeVersion)return;
  const attempt=state.attempts.find(a=>a.id===id);
  if(!attempt)throw new Error('This attempt was not found in your account.');
  if(completed(attempt)){document.querySelector('#page').innerHTML=`${header('This chapter is complete.','You’ve already submitted this attempt.')}<section class="panel">${empty('All done','You can find your best score on the quiz leaderboard.',`<a class="btn btn-primary" href="#leaderboard/${attempt.quizId}">View leaderboard ${icon('arrow')}</a>`)}</section>`;return;}
  const quiz=state.quizzes.find(q=>q.id===attempt.quizId);
  const deadline=quiz?asUtc(attempt.startedAt).getTime()+quiz.timeLimitMinutes*60000:null;
  if(deadline&&Date.now()>=deadline){expiredAttemptPage(attempt,quiz);return;}
  const questions=await api.attemptQuestions(id);if(version!==state.routeVersion)return;
  let active=state.active;
  if(active?.attempt.id!==id){const saved=readAnswers(id);active={attempt,quiz,questions,index:0,saved,draft:structuredClone(saved),dirty:new Set(),deadline,submitting:false,saving:false,resumed:Object.keys(saved).length>0||Date.now()-asUtc(attempt.startedAt).getTime()>20000};}
  else {active.attempt=attempt;active.questions=questions;active.quiz=quiz;active.deadline=deadline;}
  state.active=active;
  document.querySelector('#page').innerHTML=`<a class="text-link back-link" href="#attempts">${icon('left')} My attempts</a>${header(quiz?.title||`Quiz #${attempt.quizId}`,'Take your time. You’ve got this.',`<span class="timer" id="timer" role="timer">${icon('clock')}<span id="time-left">${deadline?'--:--':'Timer unavailable'}</span></span>`,'Quiz in progress')}${!quiz?'<div class="notice">The quiz is no longer listed. Your server still checks the time limit when you save.</div>':''}${active.resumed?'<div class="notice">'+icon('info')+'<p>Previously saved answers remain submitted. This browser can show only the selections it has saved here.</p></div>':''}<div class="attempt-layout"><section class="attempt-main" id="question-area"></section><aside class="attempt-sidebar panel"><h2>Your progress</h2><p class="field-help">Saved in this browser</p><div class="question-progress"><strong id="saved-count">0</strong><span> / ${questions.length} questions</span></div><div class="progress-bar"><span id="progress-fill"></span></div><div id="question-nav" class="question-nav" aria-label="Question navigation"></div><p class="field-help">Save each answer before finishing. You can return to any question while time remains.</p>${btn('Finish quiz '+icon('check'),'finish','','secondary')}</aside></div>`;
  drawQuestion();drawProgress();updateTimer();
  if(state.route==='attempt')state.timer=setInterval(updateTimer,1000);
}
function drawQuestion() {
  const a=state.active;if(!a)return;
  const q=a.questions[a.index];
  const target=document.querySelector('#question-area');
  if(!target)return;
  if(!q){target.innerHTML=`<div class="panel">${empty('No questions in this quiz','You can submit this attempt and return to the library.',btn('Finish attempt','finish'))}</div>`;return;}
  const selected=a.draft[q.id]||[];
  target.innerHTML=`<article class="panel attempt-question"><div class="section-header"><span class="eyebrow">Question ${a.index+1} of ${a.questions.length}</span><span class="badge badge-info">${typeLabel(q.type)}</span></div><h2>${e(q.text)}</h2><p class="subtitle">${isMultiple(q)?'Choose all answers that apply.':'Choose one answer.'}</p><fieldset class="question-options"><legend class="sr-only">${e(q.text)}</legend>${q.answerOptions.map((o,index)=>`<label class="answer-choice ${selected.includes(o.id)?'selected':''}"><input type="${isMultiple(q)?'checkbox':'radio'}" name="answer" value="${o.id}" ${selected.includes(o.id)?'checked':''}><span class="answer-letter">${String.fromCharCode(65+index)}</span><span>${e(o.text)}</span><span class="answer-check">${icon('check')}</span></label>`).join('')}</fieldset><div class="attempt-actions"><span class="save-status ${a.dirty.has(q.id)?'':'saved'}" id="save-status" aria-live="polite">${a.dirty.has(q.id)?'Unsaved changes':Object.hasOwn(a.saved,q.id)?'Answer saved':'Not answered yet'}</span><div class="row-actions">${iconButton('left','Previous question','previous-question',a.index===0?'disabled':'')}${btn(a.index===a.questions.length-1?'Save answer '+icon('check'):'Save & next '+icon('arrow'),'save-next')}</div></div><div class="form-error" role="alert" hidden></div></article>`;
  target.querySelector('fieldset').addEventListener('change',()=>{
    a.draft[q.id]=[...target.querySelectorAll('input[name=answer]:checked')].map(input=>Number(input.value));a.dirty.add(q.id);
    target.querySelectorAll('.answer-choice').forEach(label=>label.classList.toggle('selected',label.querySelector('input').checked));
    document.querySelector('#save-status').textContent='Unsaved changes';document.querySelector('#save-status').classList.remove('saved');drawProgress();
  });
}
function drawProgress() {
  const a=state.active,target=document.querySelector('#question-nav');if(!a||!target)return;
  const saved=a.questions.filter(q=>(a.saved[q.id]||[]).length>0).length;
  document.querySelector('#saved-count').textContent=saved;
  document.querySelector('#progress-fill').style.width=`${a.questions.length?saved/a.questions.length*100:0}%`;
  target.innerHTML=a.questions.map((q,index)=>`<button type="button" class="question-dot ${a.index===index?'active':''} ${(a.saved[q.id]||[]).length?'answered':''}" data-action="jump-question" data-index="${index}" aria-label="Question ${index+1}${(a.saved[q.id]||[]).length?', saved':''}${a.dirty.has(q.id)?', unsaved changes':''}" ${a.index===index?'aria-current="step"':''}>${index+1}${a.dirty.has(q.id)?'<span class="dirty-dot"></span>':''}</button>`).join('');
}
async function saveCurrent(button) {
  const a=state.active,q=a?.questions[a.index];if(!q||a.submitting||a.saving)return;
  const known=Object.hasOwn(a.draft,q.id);
  if(!known){showFormError(document.querySelector('#question-area'),new Error('Choose an answer before saving.'));return;}
  const sent = [...(a.draft[q.id] || [])];
  const previousIndex = a.index;
  a.saving = true;
  busy(button,true,'Saving…');
  try{
    await api.saveAnswer(a.attempt.id,{questionId:q.id,answerOptionIds:sent});
    if(state.active !== a || !state.profile)return;
    a.saved[q.id]=sent;
    const unchanged = JSON.stringify(a.draft[q.id] || []) === JSON.stringify(sent);
    if(unchanged)a.dirty.delete(q.id);
    writeAnswers(a);
    if(a.index===previousIndex && unchanged && a.index<a.questions.length-1)a.index++;
    drawQuestion();drawProgress();
  }catch(error){showFormError(document.querySelector('#question-area'),error);}
  finally { a.saving = false; busy(button,false); }
}
function updateTimer() {
  const a=state.active;if(!a||!document.querySelector('#time-left'))return;
  if(!a.deadline)return;
  const remaining=Math.max(0,Math.ceil((a.deadline-Date.now())/1000));
  document.querySelector('#time-left').textContent=`${String(Math.floor(remaining/60)).padStart(2,'0')}:${String(remaining%60).padStart(2,'0')}`;
  document.querySelector('#timer').classList.toggle('timer-warning',remaining<=60);
  if(remaining===0){clearInterval(state.timer);state.timer=null;if(!a.submitting){expiredAttemptPage(a.attempt,a.quiz);toast('Time has ended. Submit the answers you already saved.','error');}}
}
function expiredAttemptPage(attempt,quiz) {
  clearInterval(state.timer);state.timer=null;
  if(state.active?.attempt.id!==attempt.id)state.active=null;
  state.expired=attempt;
  document.querySelector('#page').innerHTML=`${header(quiz?.title||`Quiz #${attempt.quizId}`,'Your time limit has ended.')}<section class="panel">${empty('Ready to wrap up?', 'Your saved answers are kept. Submit them to calculate your result.',btn('Submit saved answers '+icon('check'),'finish-expired',`data-id="${attempt.id}"`))}</section>`;
}
function finishDialog(expiredId) {
  const a=!expiredId||state.active?.attempt.id===expiredId?state.active:null;
  if(a?.saving){toast('Please wait for your answer to finish saving.');return;}
  const attempt=expiredId?state.attempts.find(item=>item.id===expiredId)||state.expired:a?.attempt;
  if(!attempt)return;
  const expired=Boolean(expiredId)||(a?.deadline&&Date.now()>=a.deadline);
  const dirty=!expired&&a?.dirty.size;
  confirmAction('Finish your quiz?',expired?'Your result will use the answers you saved before time ended.':dirty?'Your latest changes will be saved, then your result will be calculated. You cannot change answers after submitting.':'Your saved answers will be submitted. You cannot change answers after submitting.','Submit quiz',async()=>{
    const version=state.routeVersion;
    if(a)a.submitting=true;
    try{
      const timeEnded=Boolean(expiredId)||(a?.deadline&&Date.now()>=a.deadline);
      if(!timeEnded&&a){for(const questionId of [...a.dirty]){await api.saveAnswer(attempt.id,{questionId,answerOptionIds:a.draft[questionId]||[]});if(version!==state.routeVersion||!state.profile)return;a.saved[questionId]=[...(a.draft[questionId]||[])];a.dirty.delete(questionId);writeAnswers(a);}}
      const result=await api.finishAttempt(attempt.id);if(version!==state.routeVersion||!state.profile)return;state.result={...result,quizId:attempt.quizId,title:a?.attempt.id===attempt.id?a.quiz?.title:state.quizzes.find(q=>q.id===attempt.quizId)?.title};
      state.active=null;clearInterval(state.timer);state.timer=null;try{sessionStorage.removeItem(answerCacheKey(attempt.id));}catch{}
      location.hash='result';
    }catch(error){throw error;}finally{if(a)a.submitting=false;}
  });
}
function resultPage() {
  const result=state.result;
  if(!result){location.hash='attempts';return;}
  const percent=result.maxScore?Math.round(result.score/result.maxScore*100):0;
  document.querySelector('#page').innerHTML=`${header('Another step forward.',result.title||'You’ve completed your quiz.','','Quiz complete')}<section class="panel result-card"><span class="result-icon">${icon('trophy')}</span><span class="eyebrow">Your result</span><div class="result-score">${e(result.score)}<span> / ${e(result.maxScore)}</span></div><p class="result-percentage">${percent}%</p><h2>${percent>=80?'Nicely done.':percent>=50?'Keep that curiosity going.':'Every attempt is a chance to learn.'}</h2><p class="subtitle">Take what you’ve learned into your next challenge.</p><div class="form-actions"><a class="btn btn-primary" href="#quizzes">Explore quizzes ${icon('arrow')}</a><a class="btn btn-secondary" href="#leaderboard/${result.quizId}">View leaderboard</a></div></section>`;
}

async function performAction(action,button) {
  const id=Number(button.dataset.id);
  if(action==='theme'){toggleTheme();return;}
  if(action==='settings'){settingsDialog();return;}
  if(action==='close-modal'){modal.close();return;}
  if(action==='mobile-menu'){
    setNavigationOpen(!document.body.classList.contains('menu-open'), true);return;
  }
  if(action==='demo'){setDemo(true);location.hash='dashboard';await boot();return;}
    if (action === 'logout' || action === 'exit-demo') {
        const logOut = () => {
            if (modal.open) {
                modal.close();
            }
            setDemo(false);
            state.profile = null;
            state.active = null;
            state.result = null;
            clearTimeout(expiryTimer);
            clearInterval(state.timer);
            location.hash = 'login';
            renderAuth('login');
        };
        if (action === 'logout') {
            confirmAction(
                'Log out?',
                'Are you sure you want to log out?',
                'Yes, log out',
                logOut
            );
        } else {
            logOut();
        }
        return;
    }
  if(action==='reload'){await showRoute();return;}
  if(action==='start'){startDialog(id);return;}
  if(action==='resume'){if(state.active?.attempt.id!==id)state.active=null;location.hash=`attempt/${id}`;return;}
  if(action==='board'){location.hash=`leaderboard/${id}`;return;}
  if(action==='manage-quiz'){location.hash=`edit/${id}`;return;}
  if(action==='new-quiz'){quizDialog();return;}
  if(action==='edit-quiz'){quizDialog(state.editor?.quiz||state.quizzes.find(q=>q.id===id));return;}
  if(action==='publish'||action==='unpublish'){
    confirmAction(action==='publish'?'Publish this quiz?':'Unpublish this quiz?',action==='publish'?'Students will be able to start a new attempt. Check that your questions and correct answers are ready.':'This quiz will leave the published library. Existing attempt history will be kept.',action==='publish'?'Publish quiz':'Unpublish quiz',async()=>{await(action==='publish'?api.publishQuiz(id):api.unpublishQuiz(id));toast(action==='publish'?'Quiz published.':'Quiz unpublished.');await showRoute();});return;
  }
  if(action==='delete-quiz'){
    confirmAction('Delete this quiz?','The quiz and its questions will be removed. Quizzes with attempt history cannot be deleted.','Delete quiz',async()=>{await api.deleteQuiz(id);toast('Quiz deleted.');location.hash='manage';},true);return;
  }
  if(action==='new-question'){questionDialog(id);return;}
  if(action==='edit-question'){questionDialog(state.editor.quiz.id,state.editor.questions.find(q=>q.id===id));return;}
  if(action==='delete-question'){confirmAction('Delete this question?','Its answer options will also be removed.','Delete question',async()=>{await api.deleteQuestion(id);toast('Question deleted.');await showRoute();},true);return;}
  if(action==='new-option'){optionDialog(id);return;}
  if(action==='edit-option'){const question=state.editor.questions.find(q=>q.id===Number(button.dataset.question));optionDialog(question.id,question.options.find(o=>o.id===id));return;}
  if(action==='delete-option'){confirmAction('Delete this answer option?','This option will be removed from the question.','Delete option',async()=>{await api.deleteOption(id);toast('Answer option deleted.');await showRoute();},true);return;}
  if(action==='remove-photo'){const authVersion=state.authVersion;const viewVersion=state.routeVersion;confirmAction('Remove your photo?','Your initials will be used as your avatar.','Remove photo',async()=>{try{const profile=await api.deletePhoto();if(authVersion!==state.authVersion||!state.profile)return;state.profile=profile;if(viewVersion===state.routeVersion){renderShell();profilePage();}toast('Profile photo removed.');}catch(error){throw photoEndpointError(error);}});return;}
  if(action==='previous-question'){if(state.active.index>0){state.active.index--;drawQuestion();drawProgress();}return;}
  if(action==='jump-question'){state.active.index=Number(button.dataset.index);drawQuestion();drawProgress();return;}
  if(action==='save-next'){await saveCurrent(button);return;}
  if(action==='finish'){finishDialog();return;}
  if(action==='finish-expired'){finishDialog(id);return;}
}
document.addEventListener('click',async event=>{
  const navLink=event.target.closest('.topbar a[href^="#"]');
  if(navLink){const wasOpen=document.body.classList.contains('menu-open');setNavigationOpen(false);if(wasOpen)document.querySelector('.main-content')?.focus({preventScroll:true});}
  const authButton=event.target.closest('[data-auth]');
  if(authButton){event.preventDefault();renderAuth(authButton.dataset.auth);return;}
  const filter=event.target.closest('[data-filter]');
  if(filter){state.filter=filter.dataset.filter;attemptsPage();return;}
  const button=event.target.closest('[data-action]');
  if(!button||button.disabled)return;
  event.preventDefault();
  try{await performAction(button.dataset.action,button);}catch(error){toast(error.message,'error');}
});
document.addEventListener('change',async event=>{
  if(event.target.id==='board-quiz')loadBoard(Number(event.target.value));
  if(event.target.id==='demo-role'){
    try{state.profile=await api.demoRole(event.target.value);state.active=null;state.result=null;renderShell();location.hash='dashboard';await showRoute();}catch(error){toast(error.message,'error');}
  }
});
document.addEventListener('error',event=>{
  const image=event.target;
  if(image instanceof HTMLImageElement&&image.closest('.creator-avatar')){
    const creator=image.closest('.creator-avatar');
    creator.textContent=creator.dataset.creatorInitials||'';
    return;
  }
  if(image instanceof HTMLImageElement&&image.closest('.avatar'))image.parentElement.textContent=(state.profile?.userName||'Q').slice(0,1).toUpperCase();
},true);
document.addEventListener('keydown',event=>{if(event.key==='Escape'&&document.body.classList.contains('menu-open')){event.preventDefault();setNavigationOpen(false,true);}});
window.addEventListener('resize',()=>{if(document.body.classList.contains('menu-open'))setNavigationOpen(false,true);});
window.addEventListener('hashchange',()=>{if(state.profile)showRoute();else renderAuth(['register','verify'].includes(location.hash.slice(1))?location.hash.slice(1):'login');});
window.addEventListener('quizmaster:unauthorized',()=>{state.profile=null;state.active=null;clearInterval(state.timer);if(modal.open)modal.close();renderAuth('login');toast('Your session ended. Please sign in again.','error');});
window.addEventListener('beforeunload',event=>{if(state.active?.dirty.size){event.preventDefault();event.returnValue='';}});

async function boot() {
  const version=++state.authVersion;
  const session=getSession();
  if(!session){state.profile=null;renderAuth(['register','verify'].includes(location.hash.slice(1))?location.hash.slice(1):'login');return;}
  app.innerHTML='<main class="loading-state" role="status">Opening your workspace…</main>';
  try{
    const profile=await api.profile();
    if(version!==state.authVersion || getSession()?.token!==session.token)return;
    state.profile=profile;renderShell();
    clearTimeout(expiryTimer);
    const expires=Date.parse(session.expiresAtUtc);
    if(Number.isFinite(expires))expiryTimer=setTimeout(()=>{clearSession();window.dispatchEvent(new Event('quizmaster:unauthorized'));},Math.min(Math.max(0,expires-Date.now()),2147483647));
    await showRoute();
  }catch(error){if(version!==state.authVersion)return;state.profile=null;renderAuth('login');toast(error.message,'error');}
}
if(new URLSearchParams(location.search).get('demo')==='1'){
  setDemo(true);history.replaceState(null,'',`${location.pathname}#dashboard`);
}
boot();

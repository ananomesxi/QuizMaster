import { demoApi, demoSession } from './demo.js';

const SETTINGS_KEY = 'quizmaster.settings';
const SESSION_KEY = 'quizmaster.session';
const DEMO_KEY = 'quizmaster.demo';
const DEFAULT_SETTINGS = { apiBaseUrl: 'https://localhost:7282' };

function readStorage(storage, key, fallback) {
  try { return JSON.parse(storage.getItem(key)) ?? fallback; }
  catch { return fallback; }
}

export function getSettings() {
  return { ...DEFAULT_SETTINGS, ...readStorage(localStorage, SETTINGS_KEY, {}) };
}

export function setSettings(settings) {
  const next = { ...getSettings(), ...settings };
  let base = String(next.apiBaseUrl ?? '').trim().replace(/\/+$/, '');
  if (base) {
    let parsed;
    try { parsed = new URL(base); }
    catch { throw new Error('Enter a full API address, such as https://localhost:7282.'); }
    if (!['http:', 'https:'].includes(parsed.protocol) || parsed.username || parsed.password || parsed.search || parsed.hash) {
      throw new Error('Use an HTTP or HTTPS API address without credentials, a query, or a fragment.');
    }
    base = base.replace(/\/api$/i, '');
  }
  next.apiBaseUrl = base;
  localStorage.setItem(SETTINGS_KEY, JSON.stringify(next));
  return next;
}

// Decoding supports the existing ASP.NET claim names. It is only for the UI;
// authorization is always enforced by the API against the signed token.
export function decodeClaims(token) {
  try {
    const segment = String(token).split('.')[1];
    const normalized = segment.replace(/-/g, '+').replace(/_/g, '/');
    const decoded = atob(normalized.padEnd(Math.ceil(normalized.length / 4) * 4, '='));
    const claims = JSON.parse(new TextDecoder().decode(Uint8Array.from(decoded, character => character.charCodeAt(0))));
    const role = claims['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'] ?? claims.role ?? claims.roles ?? [];
    return {
      userId: claims['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier'] ?? claims.sub ?? claims.nameid ?? '',
      userName: claims['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name'] ?? claims.name ?? claims.unique_name ?? '',
      email: claims['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress'] ?? claims.email ?? '',
      roles: Array.isArray(role) ? role : [role],
      expiresAtUtc: claims.exp ? new Date(claims.exp * 1000).toISOString() : null
    };
  } catch { return { userId: '', userName: '', email: '', roles: [], expiresAtUtc: null }; }
}

export function getSession() {
  const session = readStorage(sessionStorage, SESSION_KEY, null);
  if (session?.expiresAtUtc && Date.parse(session.expiresAtUtc) <= Date.now()) {
    clearSession();
    return null;
  }
  return session;
}

export function setSession(response) {
  const session = { ...decodeClaims(response.token), ...response };
  sessionStorage.setItem(SESSION_KEY, JSON.stringify(session));
  return session;
}

export function clearSession() { sessionStorage.removeItem(SESSION_KEY); }
export function isDemo() { return sessionStorage.getItem(DEMO_KEY) === 'true'; }

export function setDemo(enabled) {
  clearSession();
  if (enabled) {
    sessionStorage.setItem(DEMO_KEY, 'true');
    setSession(demoSession());
  } else {
    sessionStorage.removeItem(DEMO_KEY);
  }
}

export function assetUrl(path) {
  if (!path) return '';
  // Only our in-memory demo creates these values; SVG is deliberately excluded.
  if (isDemo() && /^data:image\/(png|jpeg);base64,[A-Za-z0-9+/=]+$/.test(path) && path.length < 3000000) return path;
  try {
    const backend = new URL(getSettings().apiBaseUrl || location.origin);
    const url = new URL(path, `${backend.href.replace(/\/$/, '')}/`);
    if (!['http:', 'https:'].includes(url.protocol) || url.origin !== backend.origin) return '';
    if (!url.pathname.startsWith('/uploads/')) return '';
    return url.href;
  } catch { return ''; }
}

export class ApiError extends Error {
  constructor(message, status = 0, errors = null) {
    super(message);
    this.name = 'ApiError';
    this.status = status;
    this.errors = errors;
  }
}

async function request(path, { method = 'GET', body, anonymous = false } = {}) {
  const controller = new AbortController();
  const timeout = setTimeout(() => controller.abort(), 20000);
  try {
    const headers = { Accept: 'application/json' };
    const session = getSession();
    if (!anonymous && session?.token) headers.Authorization = `Bearer ${session.token.replace(/^Bearer\s+/i, '')}`;
    const isForm = body instanceof FormData;
    if (body !== undefined && !isForm) headers['Content-Type'] = 'application/json';
    const response = await fetch(`${getSettings().apiBaseUrl}${path}`, {
      method, headers, body: body === undefined ? undefined : isForm ? body : JSON.stringify(body),
      signal: controller.signal,
      credentials: 'omit'
    });
    const text = response.status === 204 ? '' : await response.text();
    let result = text;
    if (text && (response.headers.get('content-type') ?? '').includes('json')) {
      try { result = JSON.parse(text); } catch { /* Preserve readable server text. */ }
    }
    if (!response.ok) {
      if (response.status === 401 && !anonymous && getSession()?.token === session?.token) {
        clearSession();
        globalThis.dispatchEvent?.(new Event('quizmaster:unauthorized'));
      }
      const errors = result && typeof result === 'object' ? result.errors : null;
      const validation = errors ? Object.values(errors).flat().join(' ') : '';
      const detail = typeof result === 'object' ? result.detail || result.message || validation || result.title : result;
      const fallback = { 401: 'Please sign in to continue.', 403: 'Your account does not have permission for this action.', 404: 'The requested item was not found.' };
      throw new ApiError(detail || fallback[response.status] || `The request failed (${response.status}).`, response.status, errors);
    }
    return result === '' ? null : result;
  } catch (error) {
    if (error instanceof ApiError) throw error;
    if (error.name === 'AbortError') throw new ApiError('The API took too long to respond. Please try again.');
    throw new ApiError('Could not reach QuizMaster. Start the API, check the API address in Connection settings, and open its HTTPS address once if the browser asks you to trust the local certificate.');
  } finally { clearTimeout(timeout); }
}

const id = value => encodeURIComponent(value);
const liveApi = {
  login: body => request('/api/Auth/login', { method: 'POST', body, anonymous: true }),
  register: body => request('/api/Auth/register', { method: 'POST', body, anonymous: true }),
  verifyEmail: body => request('/api/Auth/verify-email', { method: 'POST', body, anonymous: true }),
  profile: () => request('/api/Account/me'),
  updateProfile: body => request('/api/Account/me', { method: 'PUT', body }),
  changePassword: body => request('/api/Account/change-password', { method: 'POST', body }),
  uploadPhoto: file => { const body = new FormData(); body.append('file', file); return request('/api/Account/me/photo', { method: 'PUT', body }); },
  deletePhoto: () => request('/api/Account/me/photo', { method: 'DELETE' }),
  publishedQuizzes: () => request('/api/Quizzes/published', { anonymous: !getSession() }),
  allQuizzes: () => request('/api/Quizzes'),
  quiz: quizId => request(`/api/Quizzes/${id(quizId)}`),
  createQuiz: body => request('/api/Quizzes', { method: 'POST', body }),
  updateQuiz: (quizId, body) => request(`/api/Quizzes/${id(quizId)}`, { method: 'PUT', body }),
  deleteQuiz: quizId => request(`/api/Quizzes/${id(quizId)}`, { method: 'DELETE' }),
  publishQuiz: quizId => request(`/api/Quizzes/${id(quizId)}/publish`, { method: 'PATCH' }),
  unpublishQuiz: quizId => request(`/api/Quizzes/${id(quizId)}/unpublish`, { method: 'PATCH' }),
  questions: quizId => request(`/api/quizzes/${id(quizId)}/questions`),
  question: questionId => request(`/api/questions/${id(questionId)}`),
  addQuestion: (quizId, body) => request(`/api/quizzes/${id(quizId)}/questions`, { method: 'POST', body }),
  updateQuestion: (questionId, body) => request(`/api/questions/${id(questionId)}`, { method: 'PUT', body }),
  deleteQuestion: questionId => request(`/api/questions/${id(questionId)}`, { method: 'DELETE' }),
  options: questionId => request(`/api/questions/${id(questionId)}/answer-options`),
  option: optionId => request(`/api/answer-options/${id(optionId)}`),
  addOption: (questionId, body) => request(`/api/questions/${id(questionId)}/answer-options`, { method: 'POST', body }),
  updateOption: (optionId, body) => request(`/api/answer-options/${id(optionId)}`, { method: 'PUT', body }),
  deleteOption: optionId => request(`/api/answer-options/${id(optionId)}`, { method: 'DELETE' }),
  startAttempt: quizId => request(`/api/QuizAttempts/quizzes/${id(quizId)}/attempts`, { method: 'POST' }),
  attemptQuestions: attemptId => request(`/api/QuizAttempts/attempts/${id(attemptId)}/questions`),
  saveAnswer: (attemptId, body) => request(`/api/QuizAttempts/attempts/${id(attemptId)}/answers`, { method: 'POST', body }),
  finishAttempt: attemptId => request(`/api/QuizAttempts/attempts/${id(attemptId)}/finish`, { method: 'POST' }),
  myAttempts: () => request('/api/QuizAttempts/attempts/my'),
  leaderboard: quizId => request(`/api/Quizzes/${id(quizId)}/leaderboard`),
  statistics: quizId => request(`/api/Quizzes/${id(quizId)}/statistics`),
  changeRole: (userId, body) => request(`/api/admin/users/${id(userId)}/role`, { method: 'PUT', body })
};

export const api = Object.fromEntries(Object.keys(liveApi).map(name => [name, (...args) => isDemo() ? demoApi[name](...args) : liveApi[name](...args)]));
api.demoRole = async role => {
  if (!isDemo()) throw new ApiError('Role previews are available only in demo mode.');
  const profile = await demoApi.demoRole(role);
  setSession(demoSession());
  return profile;
};

// Restore only the selected demo role after a reload. Demo changes are temporary.
if (isDemo()) {
  const role = getSession()?.roles?.[0];
  if (role) demoApi.demoRole(role);
}

// Only trusted, fixed SVG paths are interpolated into the UI.
const paths = {
  check: '<path d="m5 12 4 4L19 6"/>',
  brand: '<rect x="4" y="3" width="16" height="18" rx="3"/><path d="m8 12 3 3 6-7"/>',
  home: '<path d="m3 10 9-7 9 7v10a1 1 0 0 1-1 1h-5v-7H9v7H4a1 1 0 0 1-1-1Z"/>',
  quiz: '<rect x="5" y="3" width="14" height="18" rx="2"/><path d="M9 8h6M9 12h6M9 16h3"/>',
  attempts: '<circle cx="12" cy="12" r="9"/><path d="m7 12 3 3 7-7"/>',
  chart: '<path d="M4 20V13h4v7m2 0V8h4v12m2 0V3h4v17M3 21h18"/>',
  user: '<circle cx="12" cy="7" r="4"/><path d="M4 21v-2a8 8 0 0 1 16 0v2Z"/>',
  users: '<circle cx="9" cy="8" r="3"/><path d="M3 21v-2a6 6 0 0 1 12 0v2M16 5a3 3 0 0 1 0 6M18 14a5 5 0 0 1 3 5v2"/>',
  stack: '<path d="m3 7 9-5 9 5-9 5Zm0 5 9 5 9-5M3 17l9 5 9-5"/>',
  clipboard: '<rect x="5" y="5" width="14" height="16" rx="2"/><rect x="9" y="2" width="6" height="5" rx="2"/><path d="m8 14 2 2 3-4M14 14h2"/>',
  clock: '<circle cx="12" cy="12" r="9"/><path d="M12 7v5l3 2"/>',
  arrow: '<path d="M4 12h16m-6-6 6 6-6 6"/>',
  chevron: '<path d="m9 5 7 7-7 7"/>',
  left: '<path d="m15 5-7 7 7 7"/>',
  down: '<path d="m6 9 6 6 6-6"/>',
  logout: '<path d="M9 4H4v16h5m5-13 5 5-5 5M8 12h13"/>',
  moon: '<path d="M20.5 13a9 9 0 0 1-9.5-9.5A9 9 0 1 0 20.5 13Z"/>',
  sun: '<circle cx="12" cy="12" r="4"/><path d="M12 2v2m0 16v2M2 12h2m16 0h2M5 5l1.5 1.5m11 11L19 19M5 19l1.5-1.5m11-11L19 5"/>',
  menu: '<path d="M4 6h16M4 12h16M4 18h16"/>',
  close: '<path d="m6 6 12 12M6 18 18 6"/>',
  plus: '<path d="M12 5v14M5 12h14"/>',
  edit: '<path d="m15 4 5 5M4 20l5-1L21 7a2 2 0 0 0-5-5L4 14ZM3 21h18"/>',
  trash: '<path d="M3 6h18M9 6V3h6v3M6 6l1 15h10l1-15M10 10v7m4-7v7"/>',
  search: '<circle cx="10" cy="10" r="6"/><path d="m15 15 6 6"/>',
  settings: '<path d="M4 7h16M4 17h16"/><circle cx="9" cy="7" r="3"/><circle cx="15" cy="17" r="3"/>',
  code: '<path d="m8 6-6 6 6 6m8-12 6 6-6 6M14 3l-4 18"/>',
  math: '<rect x="4" y="3" width="16" height="18" rx="2"/><path d="M8 7h8M8 11h1m6 0h1m-8 4h1m6 0h1m-8 3h1m6 0h1"/>',
  history: '<path d="m2 8 10-6 10 6ZM3 21h18M5 10v8m5-8v8m4-8v8m5-8v8"/>',
  leaf: '<path d="M20 3C9 2 2 8 4 15s14 9 16-12ZM4 21 16 9"/>',
  lock: '<rect x="5" y="10" width="14" height="11" rx="2"/><path d="M8 10V6a4 4 0 0 1 8 0v4M12 14v3"/>',
  upload: '<path d="M12 16V3m-5 5 5-5 5 5M4 16v5h16v-5"/>',
  play: '<circle cx="12" cy="12" r="9"/><path d="m10 7 7 5-7 5Z"/>',
  info: '<circle cx="12" cy="12" r="9"/><path d="M12 11v6M12 7h.01"/>',
  trophy: '<path d="M8 3h8v6a4 4 0 0 1-8 0ZM8 5H4v3a4 4 0 0 0 4 4m8-7h4v3a4 4 0 0 1-4 4M12 13v6m-4 2h8"/>',
  refresh: '<path d="M20 4v6h-6M4 20v-6h6"/><path d="M5 8a8 8 0 0 1 13-3l2 5M4 14l2 5a8 8 0 0 0 13-3"/>'
};
export const icon = (name, extra = '') => `<svg class="icon ${extra}" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.65" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">${paths[name] || paths.quiz}</svg>`;
export const escapeHtml = value => String(value ?? '').replace(/[&<>"']/g, char => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[char]));
export const e = escapeHtml;
export const asUtc = value => new Date(/[zZ]$|[+-]\d\d:\d\d$/.test(value || '') ? value : `${value}Z`);
export const dateLabel = value => { const date = asUtc(value); return Number.isNaN(date.getTime()) ? 'Unknown date' : new Intl.DateTimeFormat(undefined, { month: 'short', day: 'numeric', year: 'numeric', hour: '2-digit', minute: '2-digit' }).format(date); };
export const completed = attempt => attempt.status === 1 || attempt.status === 'Completed';
export const quizIcon = title => /math|algebra|number/i.test(title) ? 'math' : /web|code|program|develop/i.test(title) ? 'code' : /history|world|art/i.test(title) ? 'history' : /bio|nature|science/i.test(title) ? 'leaf' : 'quiz';
export const typeLabel = type => ({ 0: 'Single choice', 1: 'Multiple choice', 2: 'True or false', SingleChoice: 'Single choice', MultipleChoice: 'Multiple choice', TrueFalse: 'True or false' }[type] || 'Question');
export const isMultiple = question => question.type === 1 || question.type === 'MultipleChoice';
export const empty = (title, text = '', action = '') => `<div class="empty-state">${icon('stack')}<h3>${e(title)}</h3>${text ? `<p>${e(text)}</p>` : ''}${action}</div>`;
export const field = (label, name, attributes = '', value = '', help = '') => `<div class="field"><label for="${name}">${e(label)}</label><input id="${name}" name="${name}" ${attributes} value="${e(value)}">${help ? `<p class="field-help">${e(help)}</p>` : ''}</div>`;
export const formError = () => '<div class="form-error" role="alert" hidden></div>';
export function showFormError(form, error) { const box = form?.querySelector('.form-error'); if (box) { box.textContent = error?.message || String(error); box.hidden = false; box.scrollIntoView({ block: 'nearest' }); } }
export function toast(message, kind = 'success') { const region = document.querySelector('#toast-region'); if(!region)return; while(region.children.length>=3)region.firstElementChild.remove(); const el = document.createElement('div'); el.className = `toast toast-${kind}`; el.setAttribute('role', kind === 'error' ? 'alert' : 'status'); el.textContent = message; region.append(el); setTimeout(() => el.remove(), kind === 'error' ? 8500 : 4500); }
export function busy(button, active, label = 'Saving…') { if (!button) return; if (active) { button.dataset.original = button.innerHTML; button.disabled = true; button.textContent = label; } else { button.disabled = false; button.innerHTML = button.dataset.original || button.innerHTML; } }

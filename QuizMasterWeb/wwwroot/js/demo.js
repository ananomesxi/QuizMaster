// This module is used only after the user explicitly chooses "Try the demo".
// All records and edits below remain in this browser's memory; no API is called.
const copy = value => structuredClone(value);
const fail = (message, status = 400) => { const error = new Error(message); error.status = status; throw error; };
let profile = { userName: 'Alex', email: 'alex@example.com', roles: ['Student'], profileImageUrl: null };
let quizzes = [
  { id: 1, title: 'Mathematics essentials', timeLimitMinutes: 20, isPublished: true, createdById: 'demo-nino', createdByUserName: 'Nino', createdByProfileImageUrl: null },
  { id: 2, title: 'Web development', timeLimitMinutes: 15, isPublished: true, createdById: 'demo-giorgi', createdByUserName: 'Giorgi', createdByProfileImageUrl: null },
  { id: 3, title: 'World history', timeLimitMinutes: 25, isPublished: true, createdById: 'demo-nino', createdByUserName: 'Nino', createdByProfileImageUrl: null },
  { id: 4, title: 'Biology basics', timeLimitMinutes: 20, isPublished: true, createdById: 'demo-alex', createdByUserName: 'Alex', createdByProfileImageUrl: null },
  { id: 5, title: 'Introduction to astronomy', timeLimitMinutes: 15, isPublished: false, createdById: 'demo-alex', createdByUserName: 'Alex', createdByProfileImageUrl: null }
];
let questions = [
  { id: 1, quizId: 1, text: 'What is the value of 12 × 8?', points: 5, type: 0 },
  { id: 2, quizId: 1, text: 'Which of these numbers are prime?', points: 5, type: 1 },
  { id: 3, quizId: 1, text: 'The angles of a triangle add up to 180 degrees.', points: 5, type: 2 },
  { id: 4, quizId: 1, text: 'What is the square root of 144?', points: 5, type: 0 },
  { id: 5, quizId: 2, text: 'Which language adds interactivity to a web page?', points: 5, type: 0 },
  { id: 6, quizId: 3, text: 'Which civilization built the Colosseum?', points: 5, type: 0 },
  { id: 7, quizId: 4, text: 'Which organelle is known as the powerhouse of the cell?', points: 5, type: 0 }
];
let options = [
  { id: 1, questionId: 1, text: '80', isCorrect: false }, { id: 2, questionId: 1, text: '96', isCorrect: true },
  { id: 3, questionId: 1, text: '108', isCorrect: false }, { id: 4, questionId: 1, text: '112', isCorrect: false },
  { id: 5, questionId: 2, text: '2', isCorrect: true }, { id: 6, questionId: 2, text: '9', isCorrect: false },
  { id: 7, questionId: 2, text: '11', isCorrect: true }, { id: 8, questionId: 2, text: '15', isCorrect: false },
  { id: 9, questionId: 3, text: 'True', isCorrect: true }, { id: 10, questionId: 3, text: 'False', isCorrect: false },
  { id: 11, questionId: 4, text: '10', isCorrect: false }, { id: 12, questionId: 4, text: '12', isCorrect: true },
  { id: 13, questionId: 4, text: '14', isCorrect: false }, { id: 14, questionId: 4, text: '16', isCorrect: false },
  { id: 15, questionId: 5, text: 'HTML', isCorrect: false }, { id: 16, questionId: 5, text: 'CSS', isCorrect: false },
  { id: 17, questionId: 5, text: 'JavaScript', isCorrect: true },
  { id: 18, questionId: 6, text: 'Roman', isCorrect: true }, { id: 19, questionId: 6, text: 'Mayan', isCorrect: false },
  { id: 20, questionId: 6, text: 'Egyptian', isCorrect: false },
  { id: 21, questionId: 7, text: 'Nucleus', isCorrect: false }, { id: 22, questionId: 7, text: 'Mitochondrion', isCorrect: true },
  { id: 23, questionId: 7, text: 'Ribosome', isCorrect: false }
];
const ago = days => new Date(Date.now() - days * 86400000).toISOString();
let attempts = [
  { id: 1, quizId: 1, startedAt: ago(1), status: 1 },
  { id: 2, quizId: 2, startedAt: ago(2), status: 1 },
  { id: 3, quizId: 3, startedAt: ago(3), status: 1 },
  { id: 4, quizId: 4, startedAt: ago(4), status: 1 },
  { id: 5, quizId: 2, startedAt: new Date().toISOString(), status: 0 },
  { id: 6, quizId: 1, startedAt: new Date().toISOString(), status: 0 }
];
let results = [
  { quizAttemptId: 1, score: 15, maxScore: 20 }, { quizAttemptId: 2, score: 5, maxScore: 5 },
  { quizAttemptId: 3, score: 5, maxScore: 5 }, { quizAttemptId: 4, score: 0, maxScore: 5 }
];
const selections = new Map();
// Preseeded completed results have scores but no saved option history. Keep
// their answer counts unknown rather than inferring completion from a score.
const historicalAttemptsWithoutSelections = new Set(attempts.filter(attempt => attempt.status === 1).map(attempt => attempt.id));
function attemptWithProgress(attempt) {
  const dto = copy(attempt);
  if (historicalAttemptsWithoutSelections.has(attempt.id)) return dto;
  const quizQuestions = questions.filter(question => question.quizId === attempt.quizId);
  dto.totalQuestions = quizQuestions.length;
  dto.answeredQuestions = quizQuestions.filter(question => (selections.get(`${attempt.id}:${question.id}`) || []).length > 0).length;
  return dto;
}
const next = items => Math.max(0, ...items.map(item => item.id)) + 1;
const get = (items, id, label) => items.find(item => item.id === Number(id)) ?? fail(`${label} was not found.`, 404);
const requireRole = (...roles) => { if (!profile.roles.some(role => roles.includes(role))) fail('Your account does not have permission for this action.', 403); };
function editable(quizId) {
  requireRole('Lecturer', 'Admin');
  const quiz = get(quizzes, quizId, 'Quiz');
  if (quiz.isPublished || attempts.some(attempt => attempt.quizId === quiz.id)) fail('A published quiz or a quiz with attempt history cannot be edited.', 409);
  return quiz;
}
const questionType = value => ({ SingleChoice: 0, MultipleChoice: 1, TrueFalse: 2 })[value] ?? Number(value);
const fixedTrueFalse = question => questionType(question.type) === 2;
function trueFalsePair(questionId) {
  const pair = options.filter(option => option.questionId === questionId);
  if (pair.length !== 2) return null;
  const trueOption = pair.find(option => option.text === 'True');
  const falseOption = pair.find(option => option.text === 'False');
  if (!trueOption || !falseOption || typeof trueOption.isCorrect !== 'boolean' ||
      typeof falseOption.isCorrect !== 'boolean' || trueOption.isCorrect === falseOption.isCorrect) return null;
  return { trueOption, falseOption };
}
function requireTrueFalseAnswer(dto) {
  if (questionType(dto.type) === 2 && typeof dto.trueFalseAnswer !== 'boolean') {
    fail('Choose whether True or False is the correct answer.');
  }
}
function setTrueFalsePair(questionId, answer, replace = false) {
  const pair = replace ? null : trueFalsePair(questionId);
  if (pair) {
    pair.trueOption.isCorrect = answer;
    pair.falseOption.isCorrect = !answer;
    return;
  }
  const firstId = next(options);
  options = options.filter(option => option.questionId !== questionId);
  options.push(
    { id: firstId, questionId, text: 'True', isCorrect: answer },
    { id: firstId + 1, questionId, text: 'False', isCorrect: !answer }
  );
}
function requireManualOptions(question) {
  if (fixedTrueFalse(question)) fail('True or false options are managed together. Edit the question to change its correct answer.', 409);
}
function activeAttempt(attemptId, enforceTime = true) {
  requireRole('Student');
  const attempt = get(attempts, attemptId, 'Attempt');
  if (attempt.status !== 0) fail('This attempt is no longer in progress.', 409);
  const quiz = get(quizzes, attempt.quizId, 'Quiz');
  if (enforceTime && Date.now() >= Date.parse(attempt.startedAt) + quiz.timeLimitMinutes * 60000) fail('The quiz time limit has expired.', 409);
  return attempt;
}

export function demoSession() {
  const expires = Math.floor(Date.now() / 1000) + 86400;
  const claims = { sub: 'demo-alex', name: profile.userName, email: profile.email, role: profile.roles[0], exp: expires };
  const encode = value => btoa(String.fromCharCode(...new TextEncoder().encode(JSON.stringify(value)))).replace(/=/g, '').replace(/\+/g, '-').replace(/\//g, '_');
  return { userId: 'demo-alex', email: profile.email, token: `${encode({ alg: 'none', typ: 'JWT' })}.${encode(claims)}.demo-only`, expiresAtUtc: new Date(expires * 1000).toISOString() };
}

export const demoApi = {
  async demoRole(role) { if (!['Student', 'Lecturer', 'Admin'].includes(role)) fail('Choose Student, Lecturer, or Admin.'); profile.roles = [role]; return copy(profile); },
  async login() { return demoSession(); },
  async register() { return 'Demo account created. This preview does not send email.'; },
  async verifyEmail() { return 'Demo email verified.'; },
  async profile() { return copy(profile); },
  async updateProfile(dto) {
    if (!dto.userName?.trim()) fail('User name is required.');
    profile.userName = dto.userName.trim();
    quizzes.filter(quiz => quiz.createdById === 'demo-alex').forEach(quiz => { quiz.createdByUserName = profile.userName; });
    return copy(profile);
  },
  async changePassword() { return null; },
  async uploadPhoto(file) {
    if (!['image/jpeg', 'image/png'].includes(file.type) || file.size > 2 * 1024 * 1024) fail('Choose a JPEG or PNG photo up to 2 MB.');
    profile.profileImageUrl = await new Promise((resolve, reject) => { const reader = new FileReader(); reader.onload = () => resolve(reader.result); reader.onerror = reject; reader.readAsDataURL(file); });
    quizzes.filter(quiz => quiz.createdById === 'demo-alex').forEach(quiz => { quiz.createdByProfileImageUrl = profile.profileImageUrl; });
    return copy(profile);
  },
  async deletePhoto() { profile.profileImageUrl = null; quizzes.filter(quiz => quiz.createdById === 'demo-alex').forEach(quiz => { quiz.createdByProfileImageUrl = null; }); return copy(profile); },
  async publishedQuizzes() { return copy(quizzes.filter(quiz => quiz.isPublished)); },
  async allQuizzes() { requireRole('Lecturer', 'Admin'); return copy(quizzes); },
  async quiz(quizId) { requireRole('Lecturer', 'Admin'); return copy(get(quizzes, quizId, 'Quiz')); },
  async createQuiz(dto) { requireRole('Lecturer', 'Admin'); const quiz = { id: next(quizzes), title: dto.title, timeLimitMinutes: Number(dto.timeLimitMinutes), isPublished: false, createdById: 'demo-alex', createdByUserName: profile.userName, createdByProfileImageUrl: profile.profileImageUrl }; quizzes.push(quiz); return copy(quiz); },
  async updateQuiz(quizId, dto) { requireRole('Lecturer', 'Admin'); const quiz = get(quizzes, quizId, 'Quiz'); Object.assign(quiz, { title: dto.title, timeLimitMinutes: Number(dto.timeLimitMinutes) }); return copy(quiz); },
  async deleteQuiz(quizId) {
    requireRole('Lecturer', 'Admin'); const quiz = get(quizzes, quizId, 'Quiz');
    if (attempts.some(attempt => attempt.quizId === quiz.id)) fail('A quiz with attempt history cannot be deleted.', 409);
    const questionIds = questions.filter(question => question.quizId === quiz.id).map(question => question.id);
    quizzes = quizzes.filter(item => item.id !== quiz.id); questions = questions.filter(item => item.quizId !== quiz.id); options = options.filter(option => !questionIds.includes(option.questionId)); return null;
  },
  async publishQuiz(quizId) {
    requireRole('Lecturer', 'Admin'); const quiz = get(quizzes, quizId, 'Quiz');
    if (questions.some(question => question.quizId === quiz.id && fixedTrueFalse(question) && !trueFalsePair(question.id))) {
      fail('True or false questions must have exactly two options named True and False, with exactly one correct answer.', 409);
    }
    quiz.isPublished = true; return null;
  },
  async unpublishQuiz(quizId) { requireRole('Lecturer', 'Admin'); get(quizzes, quizId, 'Quiz').isPublished = false; return null; },
  async questions(quizId) { requireRole('Lecturer', 'Admin'); get(quizzes, quizId, 'Quiz'); return copy(questions.filter(question => question.quizId === Number(quizId))); },
  async question(questionId) { requireRole('Lecturer', 'Admin'); return copy(get(questions, questionId, 'Question')); },
  async addQuestion(quizId, dto) {
    editable(quizId);
    requireTrueFalseAnswer(dto);
    const question = { id: next(questions), quizId: Number(quizId), text: dto.text, points: Number(dto.points), type: questionType(dto.type) };
    questions.push(question);
    if (fixedTrueFalse(question)) setTrueFalsePair(question.id, dto.trueFalseAnswer, true);
    return copy(question);
  },
  async updateQuestion(questionId, dto) {
    const question = get(questions, questionId, 'Question');
    editable(question.quizId);
    requireTrueFalseAnswer(dto);
    const wasTrueFalse = fixedTrueFalse(question);
    Object.assign(question, { text: dto.text, points: Number(dto.points), type: questionType(dto.type) });
    if (fixedTrueFalse(question)) setTrueFalsePair(question.id, dto.trueFalseAnswer, !wasTrueFalse);
    return copy(question);
  },
  async deleteQuestion(questionId) { const question = get(questions, questionId, 'Question'); editable(question.quizId); questions = questions.filter(item => item.id !== question.id); options = options.filter(item => item.questionId !== question.id); return null; },
  async options(questionId) { requireRole('Lecturer', 'Admin'); get(questions, questionId, 'Question'); return copy(options.filter(option => option.questionId === Number(questionId))); },
  async option(optionId) { requireRole('Lecturer', 'Admin'); return copy(get(options, optionId, 'Answer option')); },
  async addOption(questionId, dto) { const question = get(questions, questionId, 'Question'); editable(question.quizId); requireManualOptions(question); const option = { id: next(options), questionId: question.id, text: dto.text, isCorrect: Boolean(dto.isCorrect) }; options.push(option); return copy(option); },
  async updateOption(optionId, dto) { const option = get(options, optionId, 'Answer option'); const question = get(questions, option.questionId, 'Question'); editable(question.quizId); requireManualOptions(question); Object.assign(option, { text: dto.text, isCorrect: Boolean(dto.isCorrect) }); return copy(option); },
  async deleteOption(optionId) { const option = get(options, optionId, 'Answer option'); const question = get(questions, option.questionId, 'Question'); editable(question.quizId); requireManualOptions(question); options = options.filter(item => item.id !== option.id); return null; },
  async startAttempt(quizId) { requireRole('Student'); const quiz = get(quizzes, quizId, 'Quiz'); if (!quiz.isPublished) fail('Quiz is not published.', 409); const attempt = { id: next(attempts), quizId: quiz.id, startedAt: new Date().toISOString(), status: 0 }; attempts.push(attempt); return attemptWithProgress(attempt); },
  async attemptQuestions(attemptId) { const attempt = activeAttempt(attemptId); return questions.filter(question => question.quizId === attempt.quizId).map(question => ({ id: question.id, text: question.text, type: question.type, answerOptions: options.filter(option => option.questionId === question.id).map(option => ({ id: option.id, text: option.text })) })); },
  async saveAnswer(attemptId, dto) {
    const attempt = activeAttempt(attemptId); const question = get(questions, dto.questionId, 'Question');
    if (question.quizId !== attempt.quizId) fail("That question isn't part of this quiz.");
    const selected = [...new Set(dto.answerOptionIds.map(Number))];
    if (selected.some(id => !options.some(option => option.id === id && option.questionId === question.id))) fail("One or more selected options don't belong to this question.");
    if (question.type !== 1 && selected.length > 1) fail('Select at most one option for this question.');
    selections.set(`${attempt.id}:${question.id}`, selected); return null;
  },
  async finishAttempt(attemptId) {
    const attempt = activeAttempt(attemptId, false); const quizQuestions = questions.filter(question => question.quizId === attempt.quizId);
    const result = { quizAttemptId: attempt.id, score: 0, maxScore: quizQuestions.reduce((total, question) => total + question.points, 0) };
    for (const question of quizQuestions) { const correct = options.filter(option => option.questionId === question.id && option.isCorrect).map(option => option.id).sort((a,b) => a-b); const selected = [...(selections.get(`${attempt.id}:${question.id}`) ?? [])].sort((a,b) => a-b); if (correct.length && JSON.stringify(correct) === JSON.stringify(selected)) result.score += question.points; }
    attempt.status = 1; results.push(result); return copy(result);
  },
  async myAttempts() { requireRole('Student'); return attempts.map(attemptWithProgress); },
  async leaderboard(quizId) {
    get(quizzes, quizId, 'Quiz');
    const maxScore = questions.filter(question => question.quizId === Number(quizId)).reduce((total, question) => total + question.points, 0);
    const own = results.filter(result => attempts.find(attempt => attempt.id === result.quizAttemptId)?.quizId === Number(quizId)).sort((a,b) => b.score-a.score)[0];
    const rows = [{ userName: 'Nino', score: Math.round(maxScore * .85), maxScore }, { userName: 'Giorgi', score: Math.round(maxScore * .75), maxScore }];
    if (own) rows.push({ userName: profile.userName, score: own.score, maxScore: own.maxScore });
    return rows.sort((a,b) => b.score-a.score || a.userName.localeCompare(b.userName)).map((row,index) => ({ rank: index+1, ...row }));
  },
  async statistics(quizId) {
    requireRole('Lecturer', 'Admin'); get(quizzes, quizId, 'Quiz');
    const scores = results.filter(result => attempts.find(attempt => attempt.id === result.quizAttemptId)?.quizId === Number(quizId)).map(result => result.score);
    return { quizId: Number(quizId), attemptCount: scores.length, averageScore: scores.length ? scores.reduce((a,b) => a+b,0)/scores.length : 0, highestScore: scores.length ? Math.max(...scores) : 0, lowestScore: scores.length ? Math.min(...scores) : 0 };
  },
  async changeRole(userId, dto) { requireRole('Admin'); if (!['Student','Lecturer','Admin'].includes(dto.roleName)) fail('Role does not exist.',404); if (!userId?.trim()) fail('User ID is required.'); return null; }
};

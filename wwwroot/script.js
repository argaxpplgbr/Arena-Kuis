const API_URL = "http://localhost:5000/api";

let token = localStorage.getItem("arenaToken");
let currentUser = JSON.parse(localStorage.getItem("arenaUser")) || null;

let currentPage = "dashboard";
let currentQuiz = null;
let currentQuestionIndex = 0;
let selectedAnswers = {};
let quizTimer = null;
let quizTimeRemaining = 0;
let authMode = "login";
let teacherTab = "questions";


// auth

const authPage = document.getElementById("authPage");
const appPage = document.getElementById("appPage");
const pageContent = document.getElementById("pageContent");
const authForm = document.getElementById("authFormElement");

async function api(endpoint, options = {}) {
    const headers = {
        "Content-Type": "application/json",
        ...options.headers
    };

    if (token) {
        headers.Authorization = `Bearer ${token}`;
    }

    const response = await fetch(`${API_URL}${endpoint}`, {
        ...options,
        headers
    });

    const contentType = response.headers.get("content-type");
    const data = contentType?.includes("application/json")
        ? await response.json()
        : await response.text();

    if (!response.ok) {
        throw new Error(
            typeof data === "string"
                ? data
                : data.message || data.title || "Terjadi kesalahan."
        );
    }

    return data;
}

function showToast(message) {
    const toast = document.getElementById("toast");

    toast.textContent = message;
    toast.classList.add("show");

    setTimeout(() => toast.classList.remove("show"), 3000);
}

function escapeHTML(value) {
    return String(value ?? "")
        .replaceAll("&", "&amp;")
        .replaceAll("<", "&lt;")
        .replaceAll(">", "&gt;")
        .replaceAll('"', "&quot;")
        .replaceAll("'", "&#039;");
}

function formatDate(date) {
    return new Date(date).toLocaleDateString("id-ID", {
        day: "numeric",
        month: "short",
        year: "numeric"
    });
}

function formatTime(seconds) {
    const minutes = Math.floor(seconds / 60);
    const remaining = seconds % 60;

    return `${String(minutes).padStart(2, "0")}:${String(remaining).padStart(2, "0")}`;
}


// login

function switchAuthMode() {
    authMode = authMode === "login" ? "register" : "login";

    const isRegister = authMode === "register";

    document.getElementById("authTitle").textContent =
        isRegister ? "Buat akun Arena" : "Masuk ke Arena";

    document.getElementById("authDescription").textContent =
        isRegister
            ? "Buat akun untuk memulai perjalanan belajarmu."
            : "Masuk dan lanjutkan perjalanan belajarmu.";

    document.getElementById("namaGroup").classList.toggle(
        "hidden",
        !isRegister
    );

    document.getElementById("authSubmit").textContent =
        isRegister ? "Daftar sekarang" : "Masuk";

    document.getElementById("authSwitchText").textContent =
        isRegister ? "Sudah punya akun?" : "Belum punya akun?";

    document.getElementById("authSwitch").textContent =
        isRegister ? "Masuk" : "Daftar";

    document.getElementById("authError").textContent = "";
}

document.getElementById("authSwitch").addEventListener(
    "click",
    switchAuthMode
);

authForm.addEventListener("submit", async event => {
    event.preventDefault();

    const username = document.getElementById("usernameInput").value.trim();
    const password = document.getElementById("passwordInput").value;
    const nama = document.getElementById("namaInput").value.trim();

    const errorElement = document.getElementById("authError");
    errorElement.textContent = "";

    try {
        if (authMode === "register") {
            await api("/auth/register", {
                method: "POST",
                body: JSON.stringify({
                    Username: username,
                    Nama: nama,
                    Password: password
                })
            });

            showToast("Registrasi berhasil. Silakan login.");
            switchAuthMode();
            document.getElementById("passwordInput").value = "";
            return;
        }

        const result = await api("/auth/login", {
            method: "POST",
            body: JSON.stringify({
                Username: username,
                Password: password
            })
        });

        token = result.token;

        currentUser = {
            username: result.username,
            nama: result.nama,
            role: result.role,
            xp: result.xp
        };

        localStorage.setItem("arenaToken", token);
        localStorage.setItem("arenaUser", JSON.stringify(currentUser));

        openApplication();
    } catch (error) {
        errorElement.textContent = error.message;
    }
});

function openApplication() {
    authPage.classList.add("hidden");
    appPage.classList.remove("hidden");

    updateUserInformation();
    navigateTo("dashboard");
}

function updateUserInformation() {
    const nama = currentUser.nama || currentUser.username;
    const initial = nama.charAt(0).toUpperCase();

    document.getElementById("userName").textContent = nama;
    document.getElementById("userRole").textContent = currentUser.role;
    document.getElementById("userAvatar").textContent = initial;
    document.getElementById("topAvatar").textContent = initial;
    document.getElementById("miniAvatar").textContent = initial;
    document.getElementById("miniName").textContent = nama;
    document.getElementById("userXP").textContent = `${currentUser.xp} XP`;

    const isTeacher = ["Teacher", "Admin"].includes(currentUser.role);

    document.getElementById("teacherMenu").classList.toggle(
        "hidden",
        !isTeacher
    );
}

document.getElementById("logoutButton").addEventListener("click", () => {
    localStorage.removeItem("arenaToken");
    localStorage.removeItem("arenaUser");

    token = null;
    currentUser = null;
    currentQuiz = null;

    clearInterval(quizTimer);

    appPage.classList.add("hidden");
    authPage.classList.remove("hidden");

    authForm.reset();
});


// navigation

document.querySelectorAll("[data-page]").forEach(button => {
    button.addEventListener("click", () => {
        navigateTo(button.dataset.page);
    });
});

async function navigateTo(page) {
    currentPage = page;

    document.querySelectorAll(".nav-item").forEach(button => {
        button.classList.toggle("active", button.dataset.page === page);
    });

    const pageNames = {
        dashboard: "Dashboard",
        quizzes: "Explore Quiz",
        classrooms: "My Classroom",
        leaderboard: "Leaderboard",
        history: "My History",
        teacher: "Teacher Studio",
        quizRoom: "Quiz Room",
        result: "Quiz Result"
    };

    document.getElementById("currentPageName").textContent =
        pageNames[page] || page;

    pageContent.innerHTML = `
        <div class="empty-state">
            <h3>Loading...</h3>
            <p>Menyiapkan halaman.</p>
        </div>
    `;

    try {
        switch (page) {
            case "dashboard":
                await renderDashboard();
                break;
            case "quizzes":
                await renderQuizzes();
                break;
            case "classrooms":
                await renderClassrooms();
                break;
            case "leaderboard":
                await renderLeaderboard();
                break;
            case "history":
                await renderHistory();
                break;
            case "teacher":
                renderTeacherStudio();
                break;
            case "quizRoom":
                renderQuizQuestion();
                break;
            case "result":
                renderQuizResult();
                break;
        }
    } catch (error) {
        pageContent.innerHTML = `
            <div class="empty-state">
                <h3>Gagal memuat halaman</h3>
                <p>${escapeHTML(error.message)}</p>
                <button class="button button-primary"
                    onclick="navigateTo('${page}')">
                    Coba lagi
                </button>
            </div>
        `;
    }
}


// dashboard

async function renderDashboard() {
    const quizzes = await api("/quizzes");
    const results = await api("/results/my-results");

    const averageScore = results.length
        ? Math.round(
            results.reduce((total, item) => total + item.score, 0)
            / results.length
        )
        : 0;

    pageContent.innerHTML = `
        <div class="page-heading">
            <div>
                <span class="content-eyebrow">YOUR PERSONAL WORKSPACE</span>
                <h1>Dashboard</h1>
                <p>Selamat datang kembali, ${escapeHTML(currentUser.nama)}.</p>
            </div>
        </div>

        <div class="stats-grid">
            <div class="stat-card">
                <span class="stat-label">TOTAL XP</span>
                <div class="stat-value stat-accent">${currentUser.xp}</div>
            </div>

            <div class="stat-card">
                <span class="stat-label">QUIZ SELESAI</span>
                <div class="stat-value">${results.length}</div>
            </div>

            <div class="stat-card">
                <span class="stat-label">RATA-RATA SKOR</span>
                <div class="stat-value">${averageScore}%</div>
            </div>
        </div>

        <div class="dashboard-grid">
            <div class="card welcome-card">
                <span class="content-eyebrow">A LITTLE CHALLENGE</span>
                <h2>Belajar nggak harus selalu serius.</h2>
                <p>Pilih kuis, jawab pertanyaan, dan kumpulkan XP-mu.</p>
                <button class="button button-primary"
                    onclick="navigateTo('quizzes')">
                    Explore quizzes →
                </button>
            </div>

            <div class="card">
                <h3 class="card-title">Recent activity</h3>
                <p class="muted">Hasil kuis terakhir kamu.</p>

                <div class="recent-list" style="margin-top:18px;">
                    ${
                        results.slice(0, 3).map(result => `
                            <div class="recent-item">
                                <div>
                                    <strong>Quiz #${result.quizId}</strong>
                                    <span>${formatDate(result.completedAt)}</span>
                                </div>
                                <div class="score-tag">${result.score}%</div>
                            </div>
                        `).join("")
                        || `<p class="muted">Belum ada aktivitas.</p>`
                    }
                </div>
            </div>
        </div>

        <div style="margin-top:25px;">
            <div class="page-heading">
                <div>
                    <span class="content-eyebrow">PICK YOUR NEXT CHALLENGE</span>
                    <h2 style="font-family:var(--heading);">Recommended quizzes</h2>
                </div>

                <button class="button button-secondary"
                    onclick="navigateTo('quizzes')">
                    View all
                </button>
            </div>

            <div class="quiz-grid">
                ${quizzes.slice(0, 2).map(quiz => quizCard(quiz)).join("")}
            </div>
        </div>
    `;
}


// quizzes

function quizCard(quiz) {
    return `
        <div class="quiz-card">
            <div class="quiz-card-top">
                <span class="quiz-tag">${escapeHTML(quiz.language)}</span>
                <span class="muted">#${quiz.id}</span>
            </div>

            <h3>${escapeHTML(quiz.title)}</h3>
            <p>${escapeHTML(quiz.description || "Tidak ada deskripsi.")}</p>

            <div class="quiz-meta">
                <span>◷ ${quiz.durationMinutes} menit</span>
                <span>▤ ${quiz.totalQuestions} soal</span>
            </div>

            <button class="button button-primary"
                onclick="startQuiz(${quiz.id})">
                Mulai quiz →
            </button>
        </div>
    `;
}

async function renderQuizzes() {
    const quizzes = await api("/quizzes");

    pageContent.innerHTML = `
        <div class="page-heading">
            <div>
                <span class="content-eyebrow">FIND YOUR NEXT CHALLENGE</span>
                <h1>Explore Quiz</h1>
                <p>Pilih kuis yang ingin kamu kerjakan.</p>
            </div>
        </div>

        <div class="quiz-grid">
            ${
                quizzes.map(quiz => quizCard(quiz)).join("")
                || `<div class="empty-state">
                    <h3>Belum ada kuis</h3>
                    <p>Kuis yang dipublikasikan akan muncul di sini.</p>
                </div>`
            }
        </div>
    `;
}

async function startQuiz(id) {
    try {
        currentQuiz = await api(`/quizzes/${id}`);

        currentQuestionIndex = 0;
        selectedAnswers = {};
        quizTimeRemaining = currentQuiz.durationMinutes * 60;

        clearInterval(quizTimer);
        navigateTo("quizRoom");
        startQuizTimer();
    } catch (error) {
        showToast(error.message);
    }
}


// quiz room

function startQuizTimer() {
    clearInterval(quizTimer);

    quizTimer = setInterval(() => {
        quizTimeRemaining--;

        const timer = document.getElementById("quizTimer");

        if (timer) {
            timer.textContent = formatTime(quizTimeRemaining);
        }

        if (quizTimeRemaining <= 0) {
            clearInterval(quizTimer);
            submitQuiz(true);
        }
    }, 1000);
}

function renderQuizQuestion() {
    if (!currentQuiz) {
        navigateTo("quizzes");
        return;
    }

    const questions = currentQuiz.questions;
    const question = questions[currentQuestionIndex];
    const progress = ((currentQuestionIndex + 1) / questions.length) * 100;
    const letters = ["A", "B", "C", "D"];

    pageContent.innerHTML = `
        <div class="page-heading">
            <div>
                <span class="content-eyebrow">QUIZ SESSION</span>
                <h1>${escapeHTML(currentQuiz.title)}</h1>
                <p>Fokus dan kerjakan dengan tenang.</p>
            </div>

            <div class="quiz-timer" id="quizTimer">
                ${formatTime(quizTimeRemaining)}
            </div>
        </div>

        <div class="quiz-progress">
            <span>Pertanyaan ${currentQuestionIndex + 1} dari ${questions.length}</span>
            <span>${Math.round(progress)}%</span>
        </div>

        <div class="progress-track">
            <div class="progress-fill" style="width:${progress}%"></div>
        </div>

        <div class="question-number">
            QUESTION ${String(currentQuestionIndex + 1).padStart(2, "0")}
        </div>

        <h2 class="question-text">
            ${escapeHTML(question.questionText)}
        </h2>

        <div class="answer-list">
            ${question.options.map((option, index) => `
                <button class="answer-option ${
                    selectedAnswers[question.id] === index ? "selected" : ""
                }" onclick="selectAnswer(${question.id}, ${index})">
                    <span class="answer-letter">${letters[index]}</span>
                    <span>${escapeHTML(option)}</span>
                </button>
            `).join("")}
        </div>

        <div class="quiz-controls">
            <button class="button button-secondary"
                onclick="previousQuestion()"
                ${currentQuestionIndex === 0 ? "disabled" : ""}>
                ← Sebelumnya
            </button>

            ${
                currentQuestionIndex === questions.length - 1
                    ? `<button class="button button-primary" onclick="submitQuiz()">
                        Selesaikan quiz
                    </button>`
                    : `<button class="button button-primary" onclick="nextQuestion()">
                        Berikutnya →
                    </button>`
            }
        </div>
    `;
}

function selectAnswer(questionId, answerIndex) {
    selectedAnswers[questionId] = answerIndex;
    renderQuizQuestion();
}

function nextQuestion() {
    if (currentQuestionIndex < currentQuiz.questions.length - 1) {
        currentQuestionIndex++;
        renderQuizQuestion();
    }
}

function previousQuestion() {
    if (currentQuestionIndex > 0) {
        currentQuestionIndex--;
        renderQuizQuestion();
    }
}

async function submitQuiz(autoSubmit = false) {
    if (!currentQuiz) return;

    if (!autoSubmit) {
        const unanswered = currentQuiz.questions.filter(
            question => selectedAnswers[question.id] === undefined
        );

        const message = unanswered.length
            ? `Masih ada ${unanswered.length} soal belum dijawab. Tetap kumpulkan?`
            : "Yakin ingin menyelesaikan kuis?";

        if (!confirm(message)) return;
    }

    clearInterval(quizTimer);

    const answers = Object.entries(selectedAnswers).map(
        ([questionId, selectedAnswer]) => ({
            QuestionId: Number(questionId),
            SelectedAnswer: selectedAnswer
        })
    );

    try {
        const result = await api(`/quizzes/${currentQuiz.id}/submit`, {
            method: "POST",
            body: JSON.stringify({ Answers: answers })
        });

        currentUser.xp = result.totalXP;

        localStorage.setItem("arenaUser", JSON.stringify(currentUser));

        document.getElementById("userXP").textContent =
            `${currentUser.xp} XP`;

        window.quizResultData = result;
        window.quizResultTitle = currentQuiz.title;

        currentQuiz = null;
        navigateTo("result");
    } catch (error) {
        showToast(error.message);
    }
}


// result

function renderQuizResult() {
    const result = window.quizResultData;

    if (!result) {
        navigateTo("dashboard");
        return;
    }

    let message;

    if (result.score >= 90) {
        message = "Excellent! Kamu menguasai materi ini.";
    } else if (result.score >= 70) {
        message = "Bagus! Sedikit lagi menuju sempurna.";
    } else if (result.score >= 50) {
        message = "Lumayan. Terus latihan ya.";
    } else {
        message = "Jangan menyerah. Coba belajar dan ulangi lagi.";
    }

    pageContent.innerHTML = `
        <div style="text-align:center;padding:25px 10px;">
            <span class="content-eyebrow">QUIZ COMPLETED</span>

            <div style="
                width:130px;height:130px;border-radius:50%;
                border:8px solid var(--pink-light);
                display:grid;place-items:center;margin:25px auto;
                background:white;
            ">
                <div>
                    <div style="font-family:var(--heading);font-size:35px;font-weight:700;">
                        ${result.score}
                    </div>
                    <div style="font-size:10px;color:var(--gray);">POINTS</div>
                </div>
            </div>

            <h1 style="font-family:var(--heading);font-size:26px;">
                ${escapeHTML(window.quizResultTitle)}
            </h1>

            <p class="muted" style="margin:12px 0 25px;">${message}</p>

            <div class="stats-grid">
                <div class="stat-card">
                    <span class="stat-label">JAWABAN BENAR</span>
                    <div class="stat-value">${result.correctAnswers}</div>
                </div>

                <div class="stat-card">
                    <span class="stat-label">TOTAL SOAL</span>
                    <div class="stat-value">${result.totalQuestions}</div>
                </div>

                <div class="stat-card">
                    <span class="stat-label">XP DIDAPAT</span>
                    <div class="stat-value stat-accent">+${result.xp}</div>
                </div>
            </div>

            <div style="display:flex;justify-content:center;gap:10px;flex-wrap:wrap;">
                <button class="button button-primary"
                    onclick="navigateTo('quizzes')">
                    Quiz lainnya
                </button>

                <button class="button button-secondary"
                    onclick="navigateTo('dashboard')">
                    Dashboard
                </button>
            </div>
        </div>
    `;
}


// classrooms

async function renderClassrooms() {
    const classrooms = await api("/classrooms");
    const isTeacher = ["Teacher", "Admin"].includes(currentUser.role);

    pageContent.innerHTML = `
        <div class="page-heading">
            <div>
                <span class="content-eyebrow">YOUR LEARNING COMMUNITY</span>
                <h1>My Classroom</h1>
                <p>${isTeacher ? "Kelola kelas yang kamu buat." : "Lihat kelas dan bergabung dengan teman."}</p>
            </div>

            <button class="button button-primary"
                onclick="${isTeacher ? "openCreateClassModal()" : "openJoinClassModal()"}">
                ${isTeacher ? "+ Buat kelas" : "+ Join class"}
            </button>
        </div>

        <div class="classroom-grid">
            ${
                classrooms.map(classroom => `
                    <div class="classroom-card">
                        <span class="content-eyebrow">CLASSROOM</span>
                        <h3 class="card-title" style="margin-top:10px;">
                            ${escapeHTML(classroom.name)}
                        </h3>
                        <div class="classroom-code">${escapeHTML(classroom.classCode)}</div>
                        <p class="muted">${classroom.studentsIds.length} siswa terdaftar</p>
                    </div>
                `).join("")
                || `<div class="empty-state">
                    <h3>Belum ada kelas</h3>
                    <p>${isTeacher ? "Buat kelas pertama untuk siswa." : "Kamu belum bergabung ke kelas."}</p>
                </div>`
            }
        </div>
    `;
}

function openCreateClassModal() {
    openModal(`
        <h2>Buat classroom</h2>
        <p class="muted">Buat kelas baru untuk siswa.</p>

        <form id="createClassForm">
            <div class="form-group" style="margin-top:20px;">
                <label>Nama kelas</label>
                <input name="name" placeholder="Contoh: X PPLG 1" required>
            </div>

            <button class="button button-primary button-full">Buat kelas</button>
        </form>
    `);

    document.getElementById("createClassForm").addEventListener("submit", async event => {
        event.preventDefault();

        const name = event.target.name.value.trim();

        try {
            await api("/classrooms", {
                method: "POST",
                body: JSON.stringify({ Name: name })
            });

            closeModal();
            showToast("Classroom berhasil dibuat.");
            renderClassrooms();
        } catch (error) {
            showToast(error.message);
        }
    });
}

function openJoinClassModal() {
    openModal(`
        <h2>Join classroom</h2>
        <p class="muted">Masukkan kode kelas dari guru kamu.</p>

        <form id="joinClassForm">
            <div class="form-group" style="margin-top:20px;">
                <label>Kode kelas</label>
                <input name="classCode" placeholder="Contoh: A12BCD" required>
            </div>

            <button class="button button-primary button-full">Bergabung</button>
        </form>
    `);

    document.getElementById("joinClassForm").addEventListener("submit", async event => {
        event.preventDefault();

        const classCode = event.target.classCode.value.trim().toUpperCase();

        try {
            await api("/classrooms/join", {
                method: "POST",
                body: JSON.stringify({ ClassCode: classCode })
            });

            closeModal();
            showToast("Berhasil bergabung!");
            renderClassrooms();
        } catch (error) {
            showToast(error.message);
        }
    });
}


// leaderboard

async function renderLeaderboard() {
    const players = await api("/results/leaderboard");

    pageContent.innerHTML = `
        <div class="page-heading">
            <div>
                <span class="content-eyebrow">THE TOP PLAYERS</span>
                <h1>Leaderboard</h1>
                <p>Siswa dengan XP tertinggi.</p>
            </div>
        </div>

        <div class="card">
            <div class="table-wrapper">
                <table class="data-table">
                    <thead>
                        <tr>
                            <th>RANK</th>
                            <th>NAMA</th>
                            <th>USERNAME</th>
                            <th>XP</th>
                        </tr>
                    </thead>

                    <tbody>
                        ${
                            players.map((player, index) => `
                                <tr>
                                    <td class="rank-number">#${index + 1}</td>
                                    <td>${escapeHTML(player.nama)}</td>
                                    <td>@${escapeHTML(player.username)}</td>
                                    <td><strong>${player.xp} XP</strong></td>
                                </tr>
                            `).join("")
                        }
                    </tbody>
                </table>
            </div>
        </div>
    `;
}


// history

async function renderHistory() {
    const results = await api("/results/my-results");

    pageContent.innerHTML = `
        <div class="page-heading">
            <div>
                <span class="content-eyebrow">YOUR PROGRESS</span>
                <h1>My History</h1>
                <p>Riwayat kuis yang pernah kamu kerjakan.</p>
            </div>
        </div>

        <div class="card">
            <div class="table-wrapper">
                <table class="data-table">
                    <thead>
                        <tr>
                            <th>QUIZ ID</th>
                            <th>SKOR</th>
                            <th>BENAR</th>
                            <th>XP</th>
                            <th>TANGGAL</th>
                        </tr>
                    </thead>

                    <tbody>
                        ${
                            results.map(result => `
                                <tr>
                                    <td>Quiz #${result.quizId}</td>
                                    <td><span class="score-tag">${result.score}%</span></td>
                                    <td>${result.correctAnswer}/${result.totalQuestions}</td>
                                    <td>+${result.xp}</td>
                                    <td>${formatDate(result.completedAt)}</td>
                                </tr>
                            `).join("")
                            || `<tr><td colspan="5">Belum ada riwayat kuis.</td></tr>`
                        }
                    </tbody>
                </table>
            </div>
        </div>
    `;
}


// teacher

function renderTeacherStudio() {
    if (!["Teacher", "Admin"].includes(currentUser.role)) {
        pageContent.innerHTML = `
            <div class="empty-state">
                <h3>Akses ditolak</h3>
                <p>Halaman ini hanya untuk guru.</p>
            </div>
        `;
        return;
    }

    pageContent.innerHTML = `
        <div class="page-heading">
            <div>
                <span class="content-eyebrow">TEACHER WORKSPACE</span>
                <h1>Studio</h1>
                <p>Buat soal dan susun kuis.</p>
            </div>
        </div>

        <div class="teacher-tabs">
            <button class="teacher-tab ${teacherTab === "questions" ? "active" : ""}"
                onclick="switchTeacherTab('questions')">
                Bank soal
            </button>

            <button class="teacher-tab ${teacherTab === "createQuestion" ? "active" : ""}"
                onclick="switchTeacherTab('createQuestion')">
                + Buat soal
            </button>

            <button class="teacher-tab ${teacherTab === "createQuiz" ? "active" : ""}"
                onclick="switchTeacherTab('createQuiz')">
                + Buat kuis
            </button>
        </div>

        <div id="teacherContent"></div>
    `;

    loadTeacherTab();
}

function switchTeacherTab(tab) {
    teacherTab = tab;
    renderTeacherStudio();
}

async function loadTeacherTab() {
    const container = document.getElementById("teacherContent");

    if (teacherTab === "questions") {
        await renderTeacherQuestions(container);
    } else if (teacherTab === "createQuestion") {
        renderCreateQuestionForm(container);
    } else if (teacherTab === "createQuiz") {
        await renderCreateQuizForm(container);
    }
}

function getCurrentUserId() {
    try {
        const payload = token.split(".")[1];
        const decoded = JSON.parse(atob(payload));

        return Number(
            decoded.nameid ||
            decoded["http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier"]
        );
    } catch {
        return null;
    }
}

function filterTeacherQuestions(questions) {
    if (currentUser.role === "Admin") return questions;

    const teacherId = getCurrentUserId();

    return questions.filter(question => question.teacherId === teacherId);
}

async function renderTeacherQuestions(container) {
    const allQuestions = await api("/questions");
    const questions = filterTeacherQuestions(allQuestions);

    container.innerHTML = `
        <div class="card">
            <h3 class="card-title">Bank soal</h3>
            <p class="muted">Daftar soal yang tersedia di database.</p>

            <div style="margin-top:20px;">
                ${
                    questions.map(question => `
                        <div class="recent-item">
                            <div style="flex:1;">
                                <strong>${escapeHTML(question.questionText)}</strong>
                                <span>${escapeHTML(question.language)} · ${escapeHTML(question.category)} · ${escapeHTML(question.difficulty)}</span>
                            </div>

                            <button class="button button-secondary"
                                onclick="deleteQuestion(${question.id})">
                                Hapus
                            </button>
                        </div>
                    `).join("")
                    || `<div class="empty-state">
                        <h3>Bank soal kosong</h3>
                        <p>Buat soal pertamamu.</p>
                    </div>`
                }
            </div>
        </div>
    `;
}


// create question

function renderCreateQuestionForm(container) {
    container.innerHTML = `
        <div class="card">
            <h3 class="card-title">Buat soal baru</h3>
            <p class="muted">Isi pertanyaan beserta empat pilihan jawaban.</p>

            <form id="createQuestionForm">
                <div class="option-input-grid">
                    <div class="form-group">
                        <label>Bahasa / materi</label>
                        <input name="language" placeholder="Contoh: C#" required>
                    </div>

                    <div class="form-group">
                        <label>Kategori</label>
                        <input name="category" placeholder="Contoh: OOP" required>
                    </div>
                </div>

                <div class="form-group">
                    <label>Pertanyaan</label>
                    <textarea name="questionText" rows="3" placeholder="Tulis pertanyaan..." required></textarea>
                </div>

                <div class="option-input-grid">
                    <div class="form-group">
                        <label>Jawaban A</label>
                        <input name="optionA" required>
                    </div>

                    <div class="form-group">
                        <label>Jawaban B</label>
                        <input name="optionB" required>
                    </div>

                    <div class="form-group">
                        <label>Jawaban C</label>
                        <input name="optionC" required>
                    </div>

                    <div class="form-group">
                        <label>Jawaban D</label>
                        <input name="optionD" required>
                    </div>
                </div>

                <div class="option-input-grid">
                    <div class="form-group">
                        <label>Jawaban benar</label>
                        <select name="correctAnswer">
                            <option value="0">A</option>
                            <option value="1">B</option>
                            <option value="2">C</option>
                            <option value="3">D</option>
                        </select>
                    </div>

                    <div class="form-group">
                        <label>Tingkat kesulitan</label>
                        <select name="difficulty">
                            <option>Easy</option>
                            <option>Medium</option>
                            <option>Hard</option>
                        </select>
                    </div>
                </div>

                <button class="button button-primary">Simpan soal</button>
            </form>
        </div>
    `;

    document.getElementById("createQuestionForm").addEventListener("submit", async event => {
        event.preventDefault();

        const data = new FormData(event.target);

        const question = {
            Language: data.get("language"),
            Category: data.get("category"),
            QuestionText: data.get("questionText"),
            OptionA: data.get("optionA"),
            OptionB: data.get("optionB"),
            OptionC: data.get("optionC"),
            OptionD: data.get("optionD"),
            CorrectAnswer: Number(data.get("correctAnswer")),
            Difficulty: data.get("difficulty")
        };

        try {
            await api("/questions", {
                method: "POST",
                body: JSON.stringify(question)
            });

            showToast("Soal berhasil disimpan.");
            teacherTab = "questions";
            renderTeacherStudio();
        } catch (error) {
            showToast(error.message);
        }
    });
}

async function deleteQuestion(id) {
    if (!confirm("Yakin ingin menghapus soal ini?")) return;

    try {
        await api(`/questions/${id}`, {
            method: "DELETE"
        });

        showToast("Soal berhasil dihapus.");
        loadTeacherTab();
    } catch (error) {
        showToast(error.message);
    }
}


// create quiz

async function renderCreateQuizForm(container) {
    const allQuestions = await api("/questions");
    const questions = filterTeacherQuestions(allQuestions);

    container.innerHTML = `
        <div class="card">
            <h3 class="card-title">Buat kuis baru</h3>
            <p class="muted">Pilih soal yang ingin digunakan untuk kuis ini.</p>

            <form id="createQuizForm">
                <div class="form-group" style="margin-top:20px;">
                    <label>Judul kuis</label>
                    <input name="title" placeholder="Contoh: Dasar C#" required>
                </div>

                <div class="form-group">
                    <label>Deskripsi</label>
                    <textarea name="description" rows="2" placeholder="Deskripsi kuis..."></textarea>
                </div>

                <div class="option-input-grid">
                    <div class="form-group">
                        <label>Materi</label>
                        <input name="language" placeholder="Contoh: C#" required>
                    </div>

                    <div class="form-group">
                        <label>Durasi (menit)</label>
                        <input name="durationMinutes" type="number" min="1" value="15" required>
                    </div>
                </div>

                <div class="form-group">
                    <label>Status publikasi</label>
                    <select name="isPublished">
                        <option value="true">Published</option>
                        <option value="false">Draft</option>
                    </select>
                </div>

                <div style="margin:20px 0;">
                    <label style="font-weight:700;font-size:13px;">Pilih soal</label>
                    <p class="muted" style="margin:5px 0 12px;">Centang soal yang akan dimasukkan.</p>

                    ${
                        questions.map(question => `
                            <label class="question-select-item">
                                <input type="checkbox" name="questionIds" value="${question.id}">
                                <div>
                                    <strong>${escapeHTML(question.questionText)}</strong>
                                    <span>${escapeHTML(question.language)} · ${escapeHTML(question.category)}</span>
                                </div>
                            </label>
                        `).join("")
                        || `<p class="muted">Belum ada soal. Buat soal terlebih dahulu.</p>`
                    }
                </div>

                <button class="button button-primary">Buat kuis</button>
            </form>
        </div>
    `;

    document.getElementById("createQuizForm").addEventListener("submit", async event => {
        event.preventDefault();

        const form = event.target;
        const data = new FormData(form);

        const questionIds = [...form.querySelectorAll(
            'input[name="questionIds"]:checked'
        )].map(input => Number(input.value));

        if (!questionIds.length) {
            showToast("Pilih minimal satu soal.");
            return;
        }

        const quiz = {
            Title: data.get("title"),
            Description: data.get("description"),
            Language: data.get("language"),
            DurationMinutes: Number(data.get("durationMinutes")),
            QuestionIds: questionIds,
            IsPublished: data.get("isPublished") === "true"
        };

        try {
            await api("/quizzes", {
                method: "POST",
                body: JSON.stringify(quiz)
            });

            showToast("Kuis berhasil dibuat.");
            navigateTo("quizzes");
        } catch (error) {
            showToast(error.message);
        }
    });
}


// modal

function openModal(content) {
    document.getElementById("modalContent").innerHTML = content;
    document.getElementById("modal").classList.remove("hidden");
}

function closeModal() {
    document.getElementById("modal").classList.add("hidden");
}

document.getElementById("modalClose").addEventListener(
    "click",
    closeModal
);

document.querySelector(".modal-backdrop").addEventListener(
    "click",
    closeModal
);


// clock

function updateClock() {
    document.getElementById("footerClock").textContent =
        new Date().toLocaleTimeString("id-ID", {
            hour: "2-digit",
            minute: "2-digit"
        });
}

updateClock();
setInterval(updateClock, 1000);


// start

if (token && currentUser) {
    openApplication();
} else {
    authPage.classList.remove("hidden");
    appPage.classList.add("hidden");
}
// --- Config & State ---
const API = '';
const PS = 10; // Page size
let currentUserData = {
    name: 'User',
    photoUrl: '',
    role: ''
};

// --- Auth Helpers ---
const getToken = () => localStorage.getItem('bank_token');
const getUserId = () => localStorage.getItem('bank_user_id');
const getRole = () => localStorage.getItem('bank_role');
function logout() {
    localStorage.clear();
    router('login');
}

// --- JWT Parser ---
function parseJwt(t) {
    const b = t.split('.')[1].replace(/-/g, '+').replace(/_/g, '/');
    const j = decodeURIComponent(atob(b).split('').map(c => '%' + ('00' + c.charCodeAt(0).toString(16)).slice(-2)).join(''));
    return JSON.parse(j);
}

// --- API Helper ---
async function api(method, path, body, qp) {
    const url = API + path + (qp ? '?' + new URLSearchParams(qp) : '');
    const h = { 'Content-Type': 'application/json' };
    const tk = getToken();
    if (tk) h['Authorization'] = 'Bearer ' + tk;
    
    const opts = { method, headers: h };
    if (body && (method === 'POST' || method === 'PUT')) opts.body = JSON.stringify(body);
    
    let res;
    try {
        res = await fetch(url, opts);
    } catch (e) {
        throw new Error('Could not reach the server. Is the API running?');
    }
    
    if (res.status === 401) {
        localStorage.clear();
        router('login');
        throw new Error('Session expired');
    }
    
    if (res.status === 204 || (res.status === 200 && res.headers.get('content-length') === '0')) return null;
    
    const data = await res.json().catch(() => null);
    if (!res.ok) throw new Error(data?.detail || data?.title || 'Request failed');
    return data;
}

// --- Image Upload Handler ---
async function uploadImage(file) {
    const url = API + '/api/Upload/profile-image';
    const fd = new FormData();
    fd.append('file', file);
    
    const tk = getToken();
    const h = {};
    if (tk) h['Authorization'] = 'Bearer ' + tk;
    
    const res = await fetch(url, {
        method: 'POST',
        headers: h,
        body: fd
    });
    
    if (!res.ok) {
        const errText = await res.text();
        throw new Error(errText || 'Upload failed');
    }
    
    return await res.json(); // returns { url: "/uploads/profile-images/..." }
}

// --- Formatters & Lookup mappings ---
const fmt = n => new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD' }).format(n || 0);
const fmtDate = d => d ? new Date(d).toLocaleDateString('en-US', { year: 'numeric', month: 'long', day: 'numeric' }) : '—';
const acctTypes = { 0: 'None', 1: 'Saving', 2: 'Current' };
const loanStatuses = { 0: 'None', 1: 'Pending', 2: 'Approved', 3: 'Rejected' };
const txnTypes = { 1: 'Transfer', 2: 'Withdraw', 3: 'Deposit', 4: 'Loan' };
const $main = () => document.getElementById('main-content');

// --- Initials Generator Fallback for Avatars ---
function getInitials(name) {
    if (!name) return 'U';
    return name.split(' ').map(n => n[0]).slice(0, 2).join('').toUpperCase();
}

function renderAvatar(photoUrl, name, cssClass = 'sidebar-user-avatar') {
    if (photoUrl) {
        return `<img src="${photoUrl}" alt="${name}" class="${cssClass}" onerror="this.outerHTML='<div class=\\'${cssClass}\\'>${getInitials(name)}</div>'">`;
    }
    return `<div class="${cssClass}">${getInitials(name)}</div>`;
}

// --- Toast notification ---
function toast(msg, type = 'success') {
    const c = document.getElementById('toast-container');
    const t = document.createElement('div');
    t.className = 'toast toast-' + type;
    t.innerHTML = `<i class="fa ${type === 'success' ? 'fa-circle-check' : 'fa-circle-exclamation'}" style="margin-right:8px"></i>` + msg;
    c.appendChild(t);
    setTimeout(() => {
        t.style.opacity = '0';
        t.style.transform = 'translateY(-10px)';
        setTimeout(() => t.remove(), 300);
    }, 3500);
}

// --- Modal Popup Dialog ---
function openModal(title, bodyHTML, onSubmit) {
    const ov = document.getElementById('modal-overlay');
    ov.classList.remove('hidden');
    ov.innerHTML = `
      <div class="modal">
        <div class="modal-header">
          <h3>${title}</h3>
          <button class="modal-close" onclick="closeModal()">&times;</button>
        </div>
        <form id="mf" onsubmit="return false;">
          <div class="modal-body">${bodyHTML}</div>
          <div class="modal-footer">
            <button type="button" class="btn btn-secondary" onclick="closeModal()">Cancel</button>
            <button type="submit" class="btn btn-primary" id="modal-submit-btn">Save</button>
          </div>
        </form>
      </div>
    `;
    
    // Bind real image file inputs if they exist in the modal body
    const fileIn = document.getElementById('modal-photo-file');
    if (fileIn) {
        fileIn.onchange = async e => {
            const file = e.target.files[0];
            if (!file) return;
            const preview = document.getElementById('modal-preview-avatar');
            preview.innerHTML = '<i class="fa fa-spinner fa-spin"></i>';
            try {
                const res = await uploadImage(file);
                document.getElementById('modal-photo-url').value = res.url;
                preview.innerHTML = `<img src="${res.url}" style="width:100%;height:100%;object-fit:cover;border-radius:50%">`;
                toast('Image uploaded successfully');
            } catch (err) {
                preview.innerHTML = '<i class="fa fa-circle-user"></i>';
                toast(err.message, 'error');
            }
        };
    }

    document.getElementById('mf').onsubmit = async e => {
        e.preventDefault();
        const btn = document.getElementById('modal-submit-btn');
        setButtonLoading(btn, true);
        try {
            await onSubmit(Object.fromEntries(new FormData(e.target)));
            closeModal();
        } catch (err) {
            toast(err.message, 'error');
        } finally {
            setButtonLoading(btn, false);
        }
    };
    
    ov.onclick = e => { if (e.target === ov) closeModal(); };
    document.onkeydown = e => { if (e.key === 'Escape') closeModal(); };
    bindPasswordToggles(ov);
}

function closeModal() {
    const ov = document.getElementById('modal-overlay');
    ov.classList.add('hidden');
    ov.innerHTML = '';
}

// --- Helpers ---
function setButtonLoading(b, l) {
    if (l) {
        b.dataset.t = b.innerHTML;
        b.innerHTML = '<span class="btn-spinner"></span>';
        b.disabled = true;
    } else {
        b.innerHTML = b.dataset.t || 'Save';
        b.disabled = false;
    }
}

function pgHTML(p, more, fn) {
    return `
    <div class="pagination">
      <button class="btn btn-sm btn-secondary" ${p <= 1 ? 'disabled' : ''} onclick="${fn}(${p - 1})">← Previous</button>
      <span class="page-info">Page ${p}</span>
      <button class="btn btn-sm btn-secondary" ${!more ? 'disabled' : ''} onclick="${fn}(${p + 1})">Next →</button>
    </div>`;
}

function tableLoader(cols = 5) {
    return `
    <table class="data-table">
      <thead><tr>${'<th>&nbsp;</th>'.repeat(cols)}</tr></thead>
      <tbody>
        ${`<tr class="skeleton-row">${'<td>&nbsp;</td>'.repeat(cols)}</tr>`.repeat(4)}
      </tbody>
    </table>`;
}

function passwordToggleBtn() {
    return `<button type="button" class="password-toggle" aria-label="Show password" tabindex="-1"><i class="fa fa-eye" aria-hidden="true"></i></button>`;
}

function bindPasswordToggles(root = document) {
    const scope = root instanceof Element ? root : document;
    scope.querySelectorAll('.password-toggle:not([data-bound])').forEach(btn => {
        btn.dataset.bound = '1';
        btn.addEventListener('click', () => {
            const input = btn.closest('.form-group-float, .password-input-wrap')?.querySelector('input');
            if (!input) return;
            const show = input.type === 'password';
            input.type = show ? 'text' : 'password';
            const icon = btn.querySelector('i');
            if (icon) icon.className = show ? 'fa fa-eye-slash' : 'fa fa-eye';
            btn.setAttribute('aria-label', show ? 'Hide password' : 'Show password');
        });
    });
}

function fi(l, n, t = 'text', v = '', r = true) {
    const req = r ? 'required' : '';
    if (t === 'password') {
        return `<div class="form-group"><label>${l}</label><div class="password-input-wrap"><input name="${n}" type="password" value="${v}" class="form-control" ${req}>${passwordToggleBtn()}</div></div>`;
    }
    return `<div class="form-group"><label>${l}</label><input name="${n}" type="${t}" value="${v}" class="form-control" ${req}></div>`;
}

function fsel(l, n, opts, sel = '') {
    return `
    <div class="form-group">
      <label>${l}</label>
      <select name="${n}" class="form-control">
        ${opts.map(([v, t]) => `<option value="${v}"${v == sel ? ' selected' : ''}>${t}</option>`).join('')}
      </select>
    </div>`;
}

// --- Nav configuration ---
const navCfg = {
    Admin: [
        { id: 'overview', label: 'Overview', icon: 'fa-chart-line' },
        { id: 'customers', label: 'Customers', icon: 'fa-users' },
        { id: 'tellers', label: 'Tellers', icon: 'fa-user-tie' },
        { id: 'branches', label: 'Branches', icon: 'fa-building' }
    ],
    Teller: [
        { id: 'lookup', label: 'Lookup Customer', icon: 'fa-magnifying-glass' },
        { id: 'operations', label: 'Account Ops', icon: 'fa-money-bill-transfer' },
        { id: 'addloan', label: 'Add Loan', icon: 'fa-file-invoice-dollar' }
    ],
    Customer: [
        { id: 'profile', label: 'My Profile', icon: 'fa-user' },
        { id: 'accounts', label: 'My Accounts', icon: 'fa-wallet' },
        { id: 'transfer', label: 'Transfer', icon: 'fa-right-left' },
        { id: 'loans', label: 'My Loans', icon: 'fa-hand-holding-dollar' }
    ]
};

// --- Router ---
async function router(view) {
    const app = document.getElementById('app');
    if (view === 'login') {
        app.innerHTML = loginHTML();
        bindLogin();
        initLandingPage();
        return;
    }
    
    const items = navCfg[view] || [];
    
    // Fetch profile details first so we can display beautiful user profile sidebar card
    try {
        const uid = getUserId();
        if (view === 'Customer') {
            const p = await api('GET', `/api/customer/${uid}/profile`);
            currentUserData = { name: p.name || 'Customer', photoUrl: p.photoUrl || '', role: 'Customer' };
        } else if (view === 'Admin') {
            currentUserData = { name: 'Super Admin', photoUrl: localStorage.getItem('bank_photo_url') || '', role: 'Administrator' };
        } else if (view === 'Teller') {
            currentUserData = { name: 'Bank Teller', photoUrl: localStorage.getItem('bank_photo_url') || '', role: 'Teller' };
        }
    } catch (e) {
        currentUserData = { name: 'Bank User', photoUrl: '', role: view };
    }

    app.innerHTML = `
      <div class="app-layout">
        <aside class="sidebar">
          <div class="sidebar-brand">
            <i class="fa fa-building-columns"></i>
            <span>BankSystem</span>
          </div>
          
          <div class="sidebar-user">
            ${renderAvatar(currentUserData.photoUrl, currentUserData.name, 'sidebar-user-avatar')}
            <div class="sidebar-user-info">
              <span class="sidebar-user-name">${currentUserData.name}</span>
              <span class="sidebar-user-role">${currentUserData.role}</span>
            </div>
          </div>
          
          <nav class="sidebar-nav">
            ${items.map(i => `<a class="nav-item" data-v="${i.id}" onclick="navigate('${i.id}')"><i class="fa ${i.icon}"></i> ${i.label}</a>`).join('')}
          </nav>
          
          <button class="logout-btn" onclick="logout()">
            <i class="fa fa-right-from-bracket"></i> Logout Portal
          </button>
        </aside>
        <main class="main-wrap" id="main-content"></main>
      </div>
    `;
    
    navigate(items[0].id);
}

async function navigate(v) {
    document.querySelectorAll('.nav-item').forEach(n => n.classList.toggle('active', n.dataset.v === v));
    $main().innerHTML = '<div class="spinner"></div>';
    const r = {
        overview: renderAdminOverview,
        customers: () => renderAdminCustomers(1, '', ''),
        tellers: () => renderAdminTellers(1, '', ''),
        branches: renderAdminBranches,
        lookup: renderTellerLookup,
        operations: renderTellerOperations,
        addloan: renderTellerAddLoan,
        profile: renderCustomerProfile,
        accounts: () => renderCustomerAccounts(1),
        transfer: renderCustomerTransfer,
        loans: renderCustomerLoans
    };
    if (r[v]) {
        try {
            await r[v]();
            pageTransition();
        } catch (e) {
            $main().innerHTML = `<p class="error">${e.message}</p>`;
        }
    }
}

// --- Dynamic Page Transitions ---
function pageTransition() {
    if (typeof gsap !== 'undefined') {
        gsap.fromTo('#main-content', { opacity: 0, y: 15 }, { opacity: 1, y: 0, duration: 0.4, ease: 'power2.out' });
    }
    if (typeof AOS !== 'undefined') {
        AOS.refresh();
    }
}

// --- Login Page HTML ---
function loginHTML() {
    return `
    <div class="landing-container">
      <canvas id="three-bg" class="three-canvas"></canvas>
      
      <!-- Floating Transparent Navbar -->
      <header class="landing-navbar">
        <div class="navbar-logo">
          <i class="fa fa-building-columns"></i>
          <span>BankSystem</span>
        </div>
        <nav class="navbar-links">
          <a href="#features">Solutions</a>
          <a href="#developers">Developers</a>
          <a href="#security">Security</a>
          <a href="#support">Support</a>
        </nav>
        <div class="navbar-badge">
          <span class="pulse-indicator"></span>
          Portal Active
        </div>
      </header>

      <!-- Split Hero Section -->
      <div class="landing-hero">
        <!-- Left: Brand Copy & Swiper Slides -->
        <div class="hero-left">
          <div class="hero-badge"><i class="fa fa-shield-halved"></i> Institutional Grade Security</div>
          <h1 class="hero-title">Next-Generation Financial Portal</h1>
          <p class="hero-desc">
            Welcome to the next generation of digital assets and banking operations. Securely manage balances, process payments, and track dynamic loan lifecycles in real-time.
          </p>

          <!-- Swiper Container -->
          <div class="swiper-container swiper">
            <div class="swiper-wrapper">
              <div class="swiper-slide">
                <div class="feature-slide-card">
                  <i class="fa fa-bolt-lightning" style="color: var(--primary)"></i>
                  <div>
                    <h4>Immediate Clearing Settlements</h4>
                    <p>Execute peer-to-peer and teller transfers instantly with dynamic ledger adjustments.</p>
                  </div>
                </div>
              </div>
              <div class="swiper-slide">
                <div class="feature-slide-card">
                  <i class="fa fa-chart-line" style="color: #10b981"></i>
                  <div>
                    <h4>Dynamic Loan & Asset Tracking</h4>
                    <p>Request, process, approve, and track loan portfolios under modular state controllers.</p>
                  </div>
                </div>
              </div>
              <div class="swiper-slide">
                <div class="feature-slide-card">
                  <i class="fa fa-user-lock" style="color: #8b5cf6"></i>
                  <div>
                    <h4>Multi-Tier Role Authorization</h4>
                    <p>Scoped dashboards for Administrators, Tellers, and Customers securing private profiles.</p>
                  </div>
                </div>
              </div>
            </div>
            <div class="swiper-pagination"></div>
          </div>

          <!-- Quick Info Stats -->
          <div class="hero-stats">
            <div class="hero-stat-item">
              <h5>99.99%</h5>
              <p>Portal Uptime</p>
            </div>
            <div class="hero-stat-item">
              <h5>&lt; 1.2s</h5>
              <p>Avg Settlement</p>
            </div>
            <div class="hero-stat-item">
              <h5>AES-256</h5>
              <p>Encryption</p>
            </div>
          </div>
        </div>

        <!-- Right: Access Card with Floating Inputs -->
        <div class="hero-right">
          <div class="login-card">
            <h2>Secure Login</h2>
            <p>Access your institutional dashboard</p>
            <div id="login-err" class="hidden error" style="margin-bottom:18px"></div>
            
            <form id="login-form">
              <div class="form-group-float">
                <input name="email" type="email" id="email-in" placeholder=" " required class="form-control-float">
                <label for="email-in"><i class="fa fa-envelope"></i> Email Address</label>
              </div>
              
              <div class="form-group-float password-field-float">
                <input name="password" type="password" id="pass-in" placeholder=" " required class="form-control-float">
                ${passwordToggleBtn()}
                <label for="pass-in"><i class="fa fa-lock"></i> Password</label>
              </div>
              
              <button type="submit" class="btn btn-primary" style="width:100%; margin-top:8px; display:flex; justify-content:space-between; align-items:center">
                <span>Sign In to Portal</span>
                <i class="fa fa-arrow-right"></i>
              </button>
            </form>
            
            <div class="login-assistance">
              <a href="#help"><i class="fa fa-circle-question"></i> Need login assistance?</a>
            </div>
          </div>
        </div>
      </div>

      <!-- Minimalist Premium Footer -->
      <footer class="landing-footer">
        <div class="footer-left">
          &copy; 2026 BankSystem Inc. All rights reserved. SEC Protected Portal.
        </div>
        <div class="footer-right">
          <a href="#privacy">Privacy Policy</a>
          <span class="footer-sep">|</span>
          <a href="#terms">Terms of Operations</a>
        </div>
      </footer>
    </div>
    `;
}

function bindLogin() {
    bindPasswordToggles(document.getElementById('login-form'));
    document.getElementById('login-form').onsubmit = async e => {
        e.preventDefault();
        const btn = e.target.querySelector('button');
        setButtonLoading(btn, true);
        const fd = Object.fromEntries(new FormData(e.target));
        try {
            const d = await api('POST', '/api/auth/login', fd);
            localStorage.setItem('bank_token', d.token);
            localStorage.setItem('bank_role', d.role);
            localStorage.setItem('bank_photo_url', d.photoUrl || '');
            localStorage.setItem('bank_user_name', d.userName || '');
            
            const claims = parseJwt(d.token);
            localStorage.setItem('bank_user_id', claims['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier']);
            router(d.role);
        } catch (err) {
            const el = document.getElementById('login-err');
            el.textContent = err.message;
            el.classList.remove('hidden');
        } finally {
            setButtonLoading(btn, false);
        }
    };
}

// --- Three.js & Swiper.js Initialization ---
function initLandingPage() {
    setTimeout(() => {
        initThreeParticles();
        if (typeof Swiper !== 'undefined') {
            new Swiper('.swiper', {
                loop: true,
                autoplay: { delay: 4000, disableOnInteraction: false },
                pagination: { el: '.swiper-pagination', clickable: true },
                effect: 'fade',
                fadeEffect: { crossFade: true }
            });
        }
        if (typeof gsap !== 'undefined') {
            gsap.from('.landing-navbar', { y: -50, opacity: 0, duration: 1, ease: 'power4.out' });
            gsap.from('.hero-badge', { scale: 0.8, opacity: 0, duration: 1, delay: 0.2, ease: 'elastic.out(1, 0.5)' });
            gsap.from('.hero-title', { x: -50, opacity: 0, duration: 1, delay: 0.3, ease: 'power3.out' });
            gsap.from('.hero-desc', { x: -50, opacity: 0, duration: 1, delay: 0.4, ease: 'power3.out' });
            gsap.from('.swiper-container', { y: 30, opacity: 0, duration: 1, delay: 0.5, ease: 'power3.out' });
            gsap.from('.hero-stats', { y: 30, opacity: 0, duration: 1, delay: 0.6, ease: 'power3.out' });
            gsap.from('.hero-right .login-card', { scale: 0.95, opacity: 0, duration: 1.2, delay: 0.4, ease: 'power4.out' });
            gsap.from('.landing-footer', { opacity: 0, duration: 1, delay: 0.8 });
        }
    }, 100);
}

function initThreeParticles() {
    const canvas = document.getElementById('three-bg');
    if (!canvas) return;
    const scene = new THREE.Scene();
    const camera = new THREE.PerspectiveCamera(75, window.innerWidth / window.innerHeight, 0.1, 1000);
    const renderer = new THREE.WebGLRenderer({ canvas: canvas, alpha: true, antialias: true });
    renderer.setSize(window.innerWidth, window.innerHeight);
    renderer.setPixelRatio(Math.min(window.devicePixelRatio, 2));

    const particlesCount = 80;
    const positions = new Float32Array(particlesCount * 3);
    const velocities = [];

    for (let i = 0; i < particlesCount * 3; i += 3) {
        positions[i] = (Math.random() - 0.5) * 10;
        positions[i + 1] = (Math.random() - 0.5) * 10;
        positions[i + 2] = (Math.random() - 0.5) * 10;
        velocities.push({
            x: (Math.random() - 0.5) * 0.003,
            y: (Math.random() - 0.5) * 0.003,
            z: (Math.random() - 0.5) * 0.003
        });
    }

    const geometry = new THREE.BufferGeometry();
    geometry.setAttribute('position', new THREE.BufferAttribute(positions, 3));

    const material = new THREE.PointsMaterial({
        size: 0.05,
        color: 0x3b82f6,
        transparent: true,
        opacity: 0.6,
        blending: THREE.AdditiveBlending
    });

    const particles = new THREE.Points(geometry, material);
    scene.add(particles);
    camera.position.z = 5;

    let mouseX = 0, mouseY = 0;
    window.addEventListener('mousemove', e => {
        mouseX = (e.clientX / window.innerWidth - 0.5) * 0.4;
        mouseY = (e.clientY / window.innerHeight - 0.5) * 0.4;
    });

    function animate() {
        requestAnimationFrame(animate);
        const pos = geometry.attributes.position.array;
        for (let i = 0; i < particlesCount; i++) {
            pos[i * 3] += velocities[i].x;
            pos[i * 3 + 1] += velocities[i].y;
            pos[i * 3 + 2] += velocities[i].z;

            if (Math.abs(pos[i * 3]) > 5) velocities[i].x *= -1;
            if (Math.abs(pos[i * 3 + 1]) > 5) velocities[i].y *= -1;
            if (Math.abs(pos[i * 3 + 2]) > 5) velocities[i].z *= -1;
        }
        geometry.attributes.position.needsUpdate = true;

        camera.position.x += (mouseX - camera.position.x) * 0.05;
        camera.position.y += (-mouseY - camera.position.y) * 0.05;
        camera.lookAt(scene.position);

        particles.rotation.y += 0.0008;
        renderer.render(scene, camera);
    }

    window.addEventListener('resize', () => {
        camera.aspect = window.innerWidth / window.innerHeight;
        camera.updateProjectionMatrix();
        renderer.setSize(window.innerWidth, window.innerHeight);
    });

    animate();
}

// --- Admin: Overview Dashboard ---
async function renderAdminOverview() {
    const m = $main();
    m.innerHTML = `
      <h2 style="margin-bottom:20px" data-aos="fade-down">Dashboard Overview</h2>
      
      <!-- Primary Core Statistics Grid -->
      <div class="stats-grid">
        ${[
            ['Total Balance', 'fa-vault', '#3b82f6', 'sv0'],
            ['Total Deposits', 'fa-arrow-down-long', '#10b981', 'sv1'],
            ['Total Withdrawals', 'fa-arrow-up-long', '#ef4444', 'sv2'],
            ['Total Loans', 'fa-file-invoice-dollar', '#8b5cf6', 'sv3'],
            ['Total Fees Collected', 'fa-percent', '#f59e0b', 'sv4']
        ].map(([l, i, c, id], x) => `
          <div class="stat-card" data-aos="fade-up" data-aos-delay="${x * 80}">
            <div class="stat-icon" style="color:${c}; background:rgba(255,255,255,0.02)"><i class="fa ${i}"></i></div>
            <div class="stat-value" id="${id}"><div class="spinner" style="width:20px;height:20px;margin:0 auto"></div></div>
            <div class="stat-label">${l}</div>
          </div>
        `).join('')}
      </div>

      <h3 style="margin:30px 0 20px 0" data-aos="fade-down">System Entity Statistics</h3>
      
      <!-- Admin Request: Total Customer, Tellers, Accounts, Cards issued cards -->
      <div class="stats-grid">
        ${[
            ['Total Registered Tellers', 'fa-user-tie', '#8b5cf6', 'sv5', '/api/admin/tellers/count'],
            ['Total Bank Customers', 'fa-users', '#10b981', 'sv6', '/api/admin/customers/count'],
            ['Active Bank Accounts', 'fa-wallet', '#3b82f6', 'sv7', '/api/admin/stats/accounts-count'],
            ['Bank Cards Issued', 'fa-credit-card', '#f59e0b', 'sv8', '/api/admin/stats/cards-count']
        ].map(([l, i, c, id, ep], x) => `
          <div class="stat-card" data-aos="fade-up" data-aos-delay="${(x + 5) * 80}">
            <div class="stat-icon" style="color:${c}; background:rgba(255,255,255,0.02)"><i class="fa ${i}"></i></div>
            <div class="stat-value" id="${id}"><div class="spinner" style="width:20px;height:20px;margin:0 auto"></div></div>
            <div class="stat-label">${l}</div>
          </div>
        `).join('')}
      </div>
    `;

    // Load Financial Stats
    const eps = ['/api/admin/stats/balance', '/api/admin/stats/deposits', '/api/admin/stats/withdrawals', '/api/admin/stats/loans', '/api/admin/stats/fees'];
    for (let i = 0; i < eps.length; i++) {
        try {
            const d = await api('GET', eps[i]);
            document.getElementById('sv' + i).textContent = fmt(Object.values(d)[0]);
        } catch (e) {
            document.getElementById('sv' + i).textContent = 'Error';
        }
    }

    // Load Entity Stats
    const entityEps = [
        '/api/admin/tellers/count',
        '/api/admin/customers/count',
        '/api/admin/stats/accounts-count',
        '/api/admin/stats/cards-count'
    ];
    for (let i = 0; i < entityEps.length; i++) {
        try {
            const d = await api('GET', entityEps[i]);
            document.getElementById('sv' + (i + 5)).textContent = d.count !== undefined ? d.count : (d.Count || 0);
        } catch (e) {
            document.getElementById('sv' + (i + 5)).textContent = '0';
        }
    }
}

// --- Admin: Customers Management ---
async function renderAdminCustomers(page = 1, search = '', status = '') {
    const m = $main();
    m.innerHTML = `
      <div class="section-header" data-aos="fade-down">
        <h2>Customers Registry</h2>
        <button class="btn btn-primary" onclick="openCustomerModal()"><i class="fa fa-plus"></i> Add Customer</button>
      </div>

      <!-- Live Search & Status Filters -->
      <div class="filter-bar" data-aos="fade-up">
        <div class="filter-search">
          <i class="fa fa-search"></i>
          <input type="text" id="cust-search" placeholder="Search by name, email or phone..." value="${search}">
        </div>
        <div class="filter-select">
          <select id="cust-status-filter">
            <option value="">All Statuses</option>
            <option value="Active" ${status === 'Active' ? 'selected' : ''}>Active</option>
            <option value="Suspended" ${status === 'Suspended' ? 'selected' : ''}>Suspended</option>
            <option value="Closed" ${status === 'Closed' ? 'selected' : ''}>Closed</option>
          </select>
        </div>
        <button class="btn btn-secondary" onclick="triggerCustomerFilter()">Filter</button>
      </div>

      <div id="ct" class="table-container" data-aos="fade-up">${tableLoader(6)}</div>
    `;

    // Bind event keys for dynamic searching
    document.getElementById('cust-search').onkeydown = e => {
        if (e.key === 'Enter') triggerCustomerFilter();
    };

    try {
        const queryParams = { page, pageSize: PS };
        if (search) queryParams.search = search;
        if (status) queryParams.status = status;

        const d = await api('GET', '/api/admin/customers', null, queryParams);
        const countRes = await api('GET', '/api/admin/customers/count', null, { search, status });
        const totalCount = countRes.count || countRes.Count || 0;
        const totalPages = Math.ceil(totalCount / PS) || 1;

        document.getElementById('ct').innerHTML = `
          <table class="data-table">
            <thead>
              <tr>
                <th>Customer Name / ID</th>
                <th>Email Address</th>
                <th>Account Status</th>
                <th>Registered Date</th>
                <th>City & Address</th>
                <th>Actions</th>
              </tr>
            </thead>
            <tbody>
              ${d.length ? d.map(c => `
                <tr>
                  <td>
                    <div class="profile-row-item">
                      ${renderAvatar(c.photoUrl, c.name, 'profile-row-img')}
                      <div class="profile-row-info">
                        <span class="profile-row-name">${c.name || ''}</span>
                        <span class="profile-row-id">ID: ${c.id}</span>
                      </div>
                    </div>
                  </td>
                  <td>${c.email || ''}</td>
                  <td>
                    <span class="status-badge status-${(c.status || 'Active').toLowerCase()}">
                      ${c.status || 'Active'}
                    </span>
                  </td>
                  <td>${fmtDate(c.createdDate)}</td>
                  <td>${c.city || '—'}, ${c.state || ''}</td>
                  <td class="actions">
                    <button class="btn btn-sm btn-accent" onclick="openCustomerModal('${c.id}')" title="Edit Customer"><i class="fa fa-pen"></i></button>
                    <button class="btn btn-sm btn-danger" onclick="delCustomer('${c.id}')" title="Delete Customer"><i class="fa fa-trash"></i></button>
                    <button class="btn btn-sm btn-secondary" onclick="openAcctModal('${c.id}')" title="Issue Card / Account"><i class="fa fa-credit-card"></i> +Acct</button>
                  </td>
                </tr>
              `).join('') : '<tr><td colspan="6" style="text-align:center">No customers found.</td></tr>'}
            </tbody>
          </table>
          <div class="pagination">
            <button class="btn btn-sm btn-secondary" ${page <= 1 ? 'disabled' : ''} onclick="changeCustomerPage(${page - 1})">← Previous</button>
            <span class="page-info">Page ${page} of ${totalPages} (Total: ${totalCount})</span>
            <button class="btn btn-sm btn-secondary" ${page >= totalPages ? 'disabled' : ''} onclick="changeCustomerPage(${page + 1})">Next →</button>
          </div>
        `;
    } catch (e) {
        document.getElementById('ct').innerHTML = `<p class="error">${e.message}</p>`;
    }
}

function triggerCustomerFilter() {
    const s = document.getElementById('cust-search').value.trim();
    const st = document.getElementById('cust-status-filter').value;
    renderAdminCustomers(1, s, st);
}

function changeCustomerPage(p) {
    const s = document.getElementById('cust-search').value.trim();
    const st = document.getElementById('cust-status-filter').value;
    renderAdminCustomers(p, s, st);
}

function customerFormHTML(c) {
    return `
      <!-- Real profile photo upload -->
      <div class="image-upload-wrapper">
        <div class="image-upload-preview" id="modal-preview-avatar">
          ${c?.photoUrl ? `<img src="${c.photoUrl}" style="width:100%;height:100%;object-fit:cover;border-radius:50%">` : '<i class="fa fa-circle-user" style="font-size: 2rem"></i>'}
        </div>
        <div class="image-upload-btn-container">
          <label>Profile Image</label>
          <input type="file" id="modal-photo-file" class="image-upload-input" accept="image/*">
          <label for="modal-photo-file" class="image-upload-trigger">
            <i class="fa fa-cloud-arrow-up"></i> Upload Photo
          </label>
          <span class="image-upload-help">JPG, PNG, GIF or WebP. Max 2MB.</span>
          <input type="hidden" name="photoUrl" id="modal-photo-url" value="${c?.photoUrl || ''}">
        </div>
      </div>

      <div class="form-row">
        ${fi('Name', 'name', 'text', c?.name || '')}
        ${fi('Email Address', 'email', 'email', c?.email || '')}
      </div>
      ${!c ? fi('Password Credentials', 'password', 'password') : ''}
      <div class="form-row">
        ${fi('Phone Number', 'phoneNumber', 'text', c?.phoneNumber || '')}
        ${fsel('Account Status', 'status', [['Active', 'Active'], ['Suspended', 'Suspended'], ['Closed', 'Closed']], c?.status || 'Active')}
      </div>
      <div class="form-row">
        ${fi('City', 'city', 'text', c?.city || '')}
        ${fi('Street Address', 'street', 'text', c?.street || '')}
      </div>
      <div class="form-row">
        ${fi('State', 'state', 'text', c?.state || '')}
        ${fi('Postal Code', 'postalCode', 'number', c?.postalCode || 0)}
      </div>
    `;
}

async function openCustomerModal(id) {
    let c = null;
    if (id) {
        try {
            c = await api('GET', `/api/admin/customers/${id}`);
        } catch (e) {
            return toast(e.message, 'error');
        }
    }
    openModal(id ? 'Edit Customer' : 'Add Customer', customerFormHTML(c), async fd => {
        fd.postalCode = parseInt(fd.postalCode) || 0;
        if (id) {
            await api('PUT', `/api/admin/customers/${id}`, fd);
            toast('Customer profile updated');
        } else {
            await api('POST', '/api/admin/customers', fd);
            toast('Customer registered successfully');
        }
        renderAdminCustomers(1);
    });
}

async function delCustomer(id) {
    if (!confirm('Are you absolutely sure you want to delete this customer? This will clear their accounts.')) return;
    try {
        await api('DELETE', `/api/admin/customers/${id}`);
        toast('Customer removed from registry');
        renderAdminCustomers(1);
    } catch (e) {
        toast(e.message, 'error');
    }
}

function openAcctModal(cid) {
    openModal('Issue Bank Account & Card', fsel('Select Account Type', 'accountType', [['1', 'Savings Account'], ['2', 'Current Account']]), async fd => {
        fd.accountType = parseInt(fd.accountType);
        try {
            await api('POST', `/api/admin/customers/${cid}/accounts`, fd);
            toast('New account generated with realistic card');
            renderAdminCustomers(1);
        } catch (err) {
            toast(err.message, 'error');
        }
    });
}

// --- Admin: Tellers Management ---
async function renderAdminTellers(page = 1, search = '', status = '') {
    const m = $main();
    m.innerHTML = `
      <div class="section-header" data-aos="fade-down">
        <h2>Tellers Registry</h2>
        <button class="btn btn-primary" onclick="openTellerModal()"><i class="fa fa-plus"></i> Add Teller</button>
      </div>

      <!-- Live Search & Status Filters -->
      <div class="filter-bar" data-aos="fade-up">
        <div class="filter-search">
          <i class="fa fa-search"></i>
          <input type="text" id="tell-search" placeholder="Search tellers by name or email..." value="${search}">
        </div>
        <div class="filter-select">
          <select id="tell-status-filter">
            <option value="">All Statuses</option>
            <option value="Active" ${status === 'Active' ? 'selected' : ''}>Active</option>
            <option value="Suspended" ${status === 'Suspended' ? 'selected' : ''}>Suspended</option>
          </select>
        </div>
        <button class="btn btn-secondary" onclick="triggerTellerFilter()">Filter</button>
      </div>

      <div id="tt" class="table-container" data-aos="fade-up">${tableLoader(5)}</div>
    `;

    document.getElementById('tell-search').onkeydown = e => {
        if (e.key === 'Enter') triggerTellerFilter();
    };

    try {
        const queryParams = { page, pageSize: PS };
        if (search) queryParams.search = search;
        if (status) queryParams.status = status;

        const d = await api('GET', '/api/admin/tellers', null, queryParams);
        const countRes = await api('GET', '/api/admin/tellers/count', null, { search, status });
        const totalCount = countRes.count || countRes.Count || 0;
        const totalPages = Math.ceil(totalCount / PS) || 1;

        document.getElementById('tt').innerHTML = `
          <table class="data-table">
            <thead>
              <tr>
                <th>Teller Name / ID</th>
                <th>Email Address</th>
                <th>Account Status</th>
                <th>Branch Code</th>
                <th>Creation Date</th>
                <th>Actions</th>
              </tr>
            </thead>
            <tbody>
              ${d.length ? d.map(t => `
                <tr>
                  <td>
                    <div class="profile-row-item">
                      ${renderAvatar(t.photoUrl, t.name, 'profile-row-img')}
                      <div class="profile-row-info">
                        <span class="profile-row-name">${t.name || ''}</span>
                        <span class="profile-row-id">ID: ${t.id}</span>
                      </div>
                    </div>
                  </td>
                  <td>${t.email || ''}</td>
                  <td>
                    <span class="status-badge status-${(t.status || 'Active').toLowerCase()}">
                      ${t.status || 'Active'}
                    </span>
                  </td>
                  <td>Branch #${t.branchFK || 'None'}</td>
                  <td>${fmtDate(t.createdDate)}</td>
                  <td class="actions">
                    <button class="btn btn-sm btn-accent" onclick="openTellerModal('${t.id}')" title="Edit Teller"><i class="fa fa-pen"></i></button>
                    <button class="btn btn-sm btn-danger" onclick="delTeller('${t.id}')" title="Delete Teller"><i class="fa fa-trash"></i></button>
                  </td>
                </tr>
              `).join('') : '<tr><td colspan="6" style="text-align:center">No tellers found.</td></tr>'}
            </tbody>
          </table>
          <div class="pagination">
            <button class="btn btn-sm btn-secondary" ${page <= 1 ? 'disabled' : ''} onclick="changeTellerPage(${page - 1})">← Previous</button>
            <span class="page-info">Page ${page} of ${totalPages} (Total: ${totalCount})</span>
            <button class="btn btn-sm btn-secondary" ${page >= totalPages ? 'disabled' : ''} onclick="changeTellerPage(${page + 1})">Next →</button>
          </div>
        `;
    } catch (e) {
        document.getElementById('tt').innerHTML = `<p class="error">${e.message}</p>`;
    }
}

function triggerTellerFilter() {
    const s = document.getElementById('tell-search').value.trim();
    const st = document.getElementById('tell-status-filter').value;
    renderAdminTellers(1, s, st);
}

function changeTellerPage(p) {
    const s = document.getElementById('tell-search').value.trim();
    const st = document.getElementById('tell-status-filter').value;
    renderAdminTellers(p, s, st);
}

function tellerFormHTML(t) {
    return `
      <!-- Real profile photo upload -->
      <div class="image-upload-wrapper">
        <div class="image-upload-preview" id="modal-preview-avatar">
          ${t?.photoUrl ? `<img src="${t.photoUrl}" style="width:100%;height:100%;object-fit:cover;border-radius:50%">` : '<i class="fa fa-circle-user" style="font-size: 2.2rem"></i>'}
        </div>
        <div class="image-upload-btn-container">
          <label>Profile Image</label>
          <input type="file" id="modal-photo-file" class="image-upload-input" accept="image/*">
          <label for="modal-photo-file" class="image-upload-trigger">
            <i class="fa fa-cloud-arrow-up"></i> Upload Photo
          </label>
          <span class="image-upload-help">JPG, PNG, GIF or WebP. Max 2MB.</span>
          <input type="hidden" name="photoUrl" id="modal-photo-url" value="${t?.photoUrl || ''}">
        </div>
      </div>

      <div class="form-row">
        ${fi('Name', 'name', 'text', t?.name || '')}
        ${fi('Email Address', 'email', 'email', t?.email || '')}
      </div>
      ${!t ? fi('Password Credentials', 'password', 'password') : ''}
      <div class="form-row">
        ${fi('Phone Number', 'phoneNumber', 'text', t?.phoneNumber || '')}
        ${fi('Branch ID Code', 'branchFK', 'number', t?.branchFK || '')}
      </div>
      <div class="form-row">
        ${fi('City', 'city', 'text', t?.city || '', false)}
        ${fi('Street Address', 'street', 'text', t?.street || '', false)}
      </div>
      <div class="form-row">
        ${fi('State', 'state', 'text', t?.state || '', false)}
        ${fi('Postal Code', 'postalCode', 'number', t?.postalCode || 0, false)}
      </div>
    `;
}

async function openTellerModal(id) {
    let t = null;
    if (id) {
        try {
            t = await api('GET', `/api/admin/tellers/${id}`);
        } catch (e) {
            return toast(e.message, 'error');
        }
    }
    openModal(id ? 'Edit Teller' : 'Add Teller', tellerFormHTML(t), async fd => {
        fd.postalCode = parseInt(fd.postalCode) || 0;
        fd.branchFK = fd.branchFK ? parseInt(fd.branchFK) : null;
        if (id) {
            await api('PUT', `/api/admin/tellers/${id}`, fd);
            toast('Teller profile updated');
        } else {
            await api('POST', '/api/admin/tellers', fd);
            toast('Teller registered successfully');
        }
        renderAdminTellers(1);
    });
}

async function delTeller(id) {
    if (!confirm('Are you sure you want to delete this teller?')) return;
    try {
        await api('DELETE', `/api/admin/tellers/${id}`);
        toast('Teller removed');
        renderAdminTellers(1);
    } catch (e) {
        toast(e.message, 'error');
    }
}

// --- Admin: Branches Management ---
async function renderAdminBranches() {
    const m = $main();
    m.innerHTML = `
      <div class="section-header" data-aos="fade-down">
        <h2>Branches Directory</h2>
        <button class="btn btn-primary" onclick="openBranchModal()"><i class="fa fa-plus"></i> Add Branch</button>
      </div>
      <div id="bt" class="table-container" data-aos="fade-up">${tableLoader(4)}</div>
    `;
    try {
        const d = await api('GET', '/api/admin/branches');
        document.getElementById('bt').innerHTML = `
          <table class="data-table">
            <thead>
              <tr>
                <th>Branch ID</th>
                <th>Branch Title</th>
                <th>Location Address</th>
                <th>Actions</th>
              </tr>
            </thead>
            <tbody>
              ${d.length ? d.map(b => `
                <tr>
                  <td>#${b.id}</td>
                  <td style="font-weight: 700; color: #ffffff">${b.name || ''}</td>
                  <td>${b.address || ''}</td>
                  <td class="actions">
                    <button class="btn btn-sm btn-accent" onclick="openBranchModal(${b.id})"><i class="fa fa-pen"></i></button>
                    <button class="btn btn-sm btn-danger" onclick="delBranch(${b.id})"><i class="fa fa-trash"></i></button>
                  </td>
                </tr>
              `).join('') : '<tr><td colspan="4" style="text-align:center">No branches added yet.</td></tr>'}
            </tbody>
          </table>
        `;
    } catch (e) {
        document.getElementById('bt').innerHTML = `<p class="error">${e.message}</p>`;
    }
}

async function openBranchModal(id) {
    let b = null;
    if (id) {
        try {
            b = await api('GET', `/api/admin/branches/${id}`);
        } catch (e) {
            return toast(e.message, 'error');
        }
    }
    openModal(id ? 'Edit Branch' : 'Add Branch', `
      ${fi('Branch Title Name', 'name', 'text', b?.name || '')}
      ${fi('Branch Full Address', 'address', 'text', b?.address || '')}
    `, async fd => {
        if (id) {
            await api('PUT', `/api/admin/branches/${id}`, fd);
            toast('Branch updated successfully');
        } else {
            await api('POST', '/api/admin/branches', fd);
            toast('Branch created successfully');
        }
        renderAdminBranches();
    });
}

async function delBranch(id) {
    if (!confirm('Delete this branch?')) return;
    try {
        await api('DELETE', `/api/admin/branches/${id}`);
        toast('Branch deleted');
        renderAdminBranches();
    } catch (e) {
        toast(e.message, 'error');
    }
}

// --- Teller: Lookup Customer ---
async function renderTellerLookup() {
    const m = $main();
    m.innerHTML = `
      <h2 style="margin-bottom:20px" data-aos="fade-down">Lookup Customer</h2>
      <div class="card" data-aos="fade-up">
        <div style="display:flex;gap:12px">
          <input id="cid" class="form-control" placeholder="Enter Customer User ID code..." style="max-width:400px">
          <button class="btn btn-primary" onclick="doLookup()"><i class="fa fa-search"></i> Search</button>
        </div>
      </div>
      <div id="lr"></div>
    `;
}

async function doLookup() {
    const id = document.getElementById('cid').value.trim();
    if (!id) return toast('Please enter a customer ID', 'error');
    const lr = document.getElementById('lr');
    lr.innerHTML = '<div class="spinner"></div>';
    try {
        const c = await api('GET', `/api/teller/customers/${id}`);
        let h = `
          <div class="card" data-aos="fade-up">
            <h3>Customer Registry Sheet</h3>
            <div class="profile-row-item" style="margin-bottom:20px">
              ${renderAvatar(c.photoUrl, c.name, 'sidebar-user-avatar')}
              <div class="profile-row-info">
                <span class="profile-row-name" style="font-size:1.15rem">${c.name || ''}</span>
                <span class="profile-row-id">Customer ID: ${c.id}</span>
              </div>
            </div>
            <dl class="info-card">
              <dt>Email Address</dt><dd>${c.email || ''}</dd>
              <dt>Phone Connection</dt><dd>${c.phoneNumber || '—'}</dd>
              <dt>City / Postal Code</dt><dd>${c.city || '—'} (${c.postalCode || 0})</dd>
              <dt>Full Street</dt><dd>${c.street || '—'}, ${c.state || ''}</dd>
            </dl>
          </div>
        `;
        try {
            const accts = await api('GET', `/api/teller/customers/${id}/accounts`, null, { page: 1, pageSize: 50 });
            h += `
              <div class="card" data-aos="fade-up">
                <h3>Customer Bank Accounts</h3>
                <div class="table-container">
                  <table class="data-table">
                    <thead><tr><th>Account ID</th><th>Account Type</th><th>Current Ledger Balance</th></tr></thead>
                    <tbody>
                      ${accts.length ? accts.map(a => `<tr><td>#${a.id}</td><td>${acctTypes[a.accountType] || a.accountType}</td><td style="font-weight:700;color:#10b981">${fmt(a.balance)}</td></tr>`).join('') : '<tr><td colspan="3">No active accounts</td></tr>'}
                    </tbody>
                  </table>
                </div>
              </div>
            `;
        } catch (e) {}
        try {
            const loans = await api('GET', `/api/teller/customers/${id}/loans`);
            h += `
              <div class="card" data-aos="fade-up">
                <h3>Loan Portfolios</h3>
                <div class="table-container">
                  <table class="data-table">
                    <thead><tr><th>Loan ID</th><th>Approved Amount</th><th>Interest</th><th>Duration</th><th>State Status</th><th>Issuance Date</th></tr></thead>
                    <tbody>
                      ${loans.length ? loans.map(l => `
                        <tr>
                          <td>#${l.id}</td>
                          <td>${fmt(l.amount)}</td>
                          <td>${l.interestRate}%</td>
                          <td>${l.durationMonths} months</td>
                          <td>
                            <span class="status-badge status-${l.status === 2 ? 'active' : l.status === 3 ? 'closed' : 'suspended'}">
                              ${loanStatuses[l.status] || l.status}
                            </span>
                          </td>
                          <td>${fmtDate(l.startDate)}</td>
                        </tr>`).join('') : '<tr><td colspan="6">No registered loans</td></tr>'}
                    </tbody>
                  </table>
                </div>
              </div>
            `;
        } catch (e) {}
        lr.innerHTML = h;
        AOS.refresh();
    } catch (e) {
        lr.innerHTML = `<p class="error">${e.message}</p>`;
    }
}

// --- Teller: Account Operations ---
let opTab = 'deposit';
async function renderTellerOperations() {
    const m = $main();
    m.innerHTML = `
      <h2 style="margin-bottom:20px" data-aos="fade-down">Account Operations</h2>
      <div class="card" style="margin-bottom:20px" data-aos="fade-up">
        <div style="display:flex;gap:16px;align-items:center;flex-wrap:wrap">
          <div style="display:flex;flex-direction:column;gap:4px;flex:1;min-width:180px">
            <label style="font-weight:600;color:var(--text-muted);text-transform:uppercase;font-size:0.8rem;letter-spacing:0.5px">Customer ID:</label>
            <input id="op-cid" class="form-control" type="number" placeholder="Enter Customer ID">
          </div>
          <div style="display:flex;flex-direction:column;gap:4px;flex:1;min-width:180px">
            <label style="font-weight:600;color:var(--text-muted);text-transform:uppercase;font-size:0.8rem;letter-spacing:0.5px">Account Type:</label>
            <select id="op-type" class="form-control">
              <option value="2">Current</option>
              <option value="1">Savings</option>
            </select>
          </div>
        </div>
      </div>
      <div class="tabs" data-aos="fade-up">
        <div class="tab active" data-t="deposit" onclick="switchOpTab('deposit')">Deposit Ledger</div>
        <div class="tab" data-t="withdraw" onclick="switchOpTab('withdraw')">Withdraw Cash</div>
        <div class="tab" data-t="transfer" onclick="switchOpTab('transfer')">Inter-ledger Transfer</div>
        <div class="tab" data-t="txns" onclick="switchOpTab('txns')">Transactions History</div>
        <div class="tab" data-t="balance" onclick="switchOpTab('balance')">Balance Inquiry</div>
      </div>
      <div id="op-content" data-aos="fade-up"></div>
    `;
    opTab = 'deposit';
    renderOpTab();
}

function switchOpTab(t) {
    opTab = t;
    document.querySelectorAll('.tab').forEach(el => el.classList.toggle('active', el.dataset.t === t));
    renderOpTab();
}

function renderOpTab() {
    const c = document.getElementById('op-content');
    if (opTab === 'deposit') {
        c.innerHTML = `<div class="card"><form onsubmit="doDeposit(event)">${fi('Deposit Amount (USD)', 'amt', 'number')}<div class="form-group"><label>Min: $0.01 | Positive values only</label></div>${fi('Reference Note', 'msg', 'text', '', false)}<button class="btn btn-primary" type="submit"><i class="fa fa-arrow-down"></i> Commit Deposit</button></form></div>`;
        // add step attribute for decimals
        setTimeout(() => { const inp = c.querySelector('input[name=amt]'); if(inp){inp.min='0.01';inp.step='0.01';} }, 50);
    } else if (opTab === 'withdraw') {
        c.innerHTML = `<div class="card"><form onsubmit="doWithdraw(event)">${fi('Withdraw Amount (USD)', 'amt', 'number')}<div class="form-group"><label>Must not exceed available balance</label></div>${fi('Reference Note', 'msg', 'text', '', false)}<button class="btn btn-primary" type="submit"><i class="fa fa-arrow-up"></i> Approve Withdrawal</button></form></div>`;
        setTimeout(() => { const inp = c.querySelector('input[name=amt]'); if(inp){inp.min='0.01';inp.step='0.01';} }, 50);
    } else if (opTab === 'transfer') {
        c.innerHTML = `
          <div class="card">
            <form onsubmit="doTellerTransfer(event)">
              ${fi('Source Account ID', 'src', 'number')}
              ${fi('Destination Account ID', 'tgt', 'number')}
              ${fi('Transfer Amount', 'amt', 'number')}
              ${fi('Transfer Reference Message', 'msg', 'text', '', false)}
              <button class="btn btn-primary" type="submit">Transfer Assets</button>
            </form>
          </div>`;
    } else if (opTab === 'txns') {
        loadOpTxns(1);
    } else if (opTab === 'balance') {
        loadOpBalance();
    }
}

async function doDeposit(e) {
    e.preventDefault();
    const cid = document.getElementById('op-cid').value;
    const type = document.getElementById('op-type').value;
    if (!cid) return toast('Please input a Customer ID', 'error');
    const fd = Object.fromEntries(new FormData(e.target));
    const amt = parseFloat(fd.amt);
    if (!amt || amt <= 0) return toast('Amount must be greater than zero', 'error');
    const btn = e.target.querySelector('button');
    setButtonLoading(btn, true);
    try {
        const r = await api('POST', `/api/teller/deposit`, null, { customerId: cid, accountType: type, amount: amt, message: fd.msg || '' });
        toast(`Deposit successful — TXN #${r.id} — Balance credited $${amt.toFixed(2)}`);
        e.target.reset();
    } catch (er) {
        toast(er.message, 'error');
    } finally {
        setButtonLoading(btn, false);
    }
}

async function doWithdraw(e) {
    e.preventDefault();
    const cid = document.getElementById('op-cid').value;
    const type = document.getElementById('op-type').value;
    if (!cid) return toast('Please input a Customer ID', 'error');
    const fd = Object.fromEntries(new FormData(e.target));
    const amt = parseFloat(fd.amt);
    if (!amt || amt <= 0) return toast('Amount must be greater than zero', 'error');
    const btn = e.target.querySelector('button');
    setButtonLoading(btn, true);
    try {
        const r = await api('POST', `/api/teller/withdraw`, null, { customerId: cid, accountType: type, amount: amt, message: fd.msg || '' });
        toast(`Withdrawal approved — TXN #${r.id} — $${amt.toFixed(2)} disbursed`);
        e.target.reset();
    } catch (er) {
        toast(er.message, 'error');
    } finally {
        setButtonLoading(btn, false);
    }
}

async function doTellerTransfer(e) {
    e.preventDefault();
    const btn = e.target.querySelector('button');
    const fd = Object.fromEntries(new FormData(e.target));
    const amt = parseFloat(fd.amt);
    if (!amt || amt <= 0) return toast('Transfer amount must be greater than zero', 'error');
    if (+fd.src === +fd.tgt) return toast('Source and destination accounts must be different', 'error');
    if (!fd.tgt || isNaN(+fd.tgt)) return toast('Please enter a valid destination Account ID', 'error');
    setButtonLoading(btn, true);
    try {
        const r = await api('POST', '/api/teller/transfer', {
            accountId: +fd.src,
            targetAccountId: +fd.tgt,
            amount: amt,
            message: fd.msg || '',
            type: 1,
            date: new Date().toISOString()
        });
        toast(`Transfer completed — TXN #${r.id} — ${fmt(amt)} moved from #${fd.src} → #${fd.tgt}`);
        e.target.reset();
    } catch (er) {
        toast(er.message, 'error');
    } finally {
        setButtonLoading(btn, false);
    }
}

async function loadOpTxns(page = 1) {
    const cid = document.getElementById('op-cid').value;
    const type = parseInt(document.getElementById('op-type').value);
    const c = document.getElementById('op-content');
    if (!cid) {
        c.innerHTML = '<div class="card"><p>Input a Customer ID to query transactions.</p></div>';
        return;
    }
    c.innerHTML = `<div class="card">${tableLoader()}</div>`;
    try {
        // Resolve account ID first
        const accounts = await api('GET', `/api/teller/customers/${cid}/accounts`, null, { page: 1, pageSize: 50 });
        const account = accounts.find(a => a.accountType === type);
        if (!account) {
            c.innerHTML = `<div class="card"><p class="error">Account of type ${acctTypes[type] || type} not found for this customer.</p></div>`;
            return;
        }

        const d = await api('GET', `/api/teller/accounts/${account.id}/transactions`, null, { page, pageSize: PS });
        c.innerHTML = `
          <div class="card">
            <h3 style="margin-bottom:16px">Transaction Ledger for Account #${account.id}</h3>
            <div class="table-container">
              <table class="data-table">
                <thead><tr><th>TXN #</th><th>Date</th><th>Type</th><th>Amount</th><th>Target Acct</th><th>Note</th></tr></thead>
                <tbody>
                  ${d.length ? d.map(t => `
                    <tr>
                      <td style="font-family:monospace;color:var(--text-muted)">#${t.id || '—'}</td>
                      <td>${fmtDate(t.date)}</td>
                      <td>
                        <span style="font-weight:700;color:${t.type === 2 ? '#ef4444' : t.type === 3 ? '#10b981' : t.type === 4 ? '#8b5cf6' : 'var(--primary)'}">
                          ${txnTypes[t.type] || t.type}
                        </span>
                      </td>
                      <td style="font-weight: 800;color:${t.type === 2 ? '#ef4444' : '#10b981'}">${t.type === 2 ? '-' : '+'}${fmt(t.amount)}</td>
                      <td>${t.targetAccountId ? '#'+t.targetAccountId : '—'}</td>
                      <td>${t.message || '—'}</td>
                    </tr>`).join('') : '<tr><td colspan="6">No historical records found.</td></tr>'}
                </tbody>
              </table>
            </div>
            ${pgHTML(page, d.length === PS, 'loadOpTxns')}
          </div>
        `;
    } catch (e) {
        c.innerHTML = `<div class="card"><p class="error">${e.message}</p></div>`;
    }
}

async function loadOpBalance() {
    const cid = document.getElementById('op-cid').value;
    const c = document.getElementById('op-content');
    if (!cid) {
        c.innerHTML = '<div class="card"><p>Input a Customer ID to check current balance.</p></div>';
        return;
    }
    c.innerHTML = '<div class="card"><div class="spinner"></div></div>';
    try {
        const accounts = await api('GET', `/api/teller/customers/${cid}/accounts`, null, { page: 1, pageSize: 50 });
        if (!accounts || accounts.length === 0) {
            c.innerHTML = `<div class="card"><p class="error">No accounts found for customer #${cid}.</p></div>`;
            return;
        }

        let balanceCardsHTML = accounts.map(a => `
          <div class="stat-card" style="flex:1; min-width:240px; margin-bottom:0">
            <div class="stat-label">${acctTypes[a.accountType] || 'Unknown'} Account #${a.id}</div>
            <div class="stat-value" style="color:#10b981;font-size:2rem;margin-top:10px">${fmt(a.balance)}</div>
            <div class="stat-desc" style="color:var(--text-muted);font-size:0.8rem;margin-top:4px">Status: ${a.accountStatus === 1 ? 'Active' : 'Suspended'}</div>
          </div>
        `).join('');

        c.innerHTML = `
          <div class="card">
            <h3 style="margin-bottom:20px">Balance Inquiry for Customer #${cid}</h3>
            <div style="display:flex; gap:16px; flex-wrap:wrap">
              ${balanceCardsHTML}
            </div>
          </div>`;
    } catch (e) {
        c.innerHTML = `<div class="card"><p class="error">${e.message}</p></div>`;
    }
}

// --- Teller: Add Loan ---
async function renderTellerAddLoan() {
    const m = $main();
    m.innerHTML = `
      <h2 style="margin-bottom:20px" data-aos="fade-down">Create Loan Order</h2>
      <div class="card" data-aos="fade-up">
        <form id="loan-form">
          ${fi('Customer ID (integer)', 'customerId', 'number')}
          <div class="form-row">
            ${fi('Loan Asset Amount', 'originalAmount', 'number')}
            ${fi('Annual Interest Rate (%)', 'interestRate', 'number')}
          </div>
          <div class="form-row">
            ${fi('Term Duration (months)', 'durationMonths', 'number')}
            ${fi('Order Start Date', 'startDate', 'date')}
          </div>
          ${fsel('Initial Loan Status', 'status', [['1', 'Pending Approval'], ['2', 'Pre-Approved & Active'], ['3', 'Declined Order']])}
          <button type="submit" class="btn btn-primary"><i class="fa fa-plus"></i> Finalize Loan Order</button>
        </form>
      </div>
    `;
    document.getElementById('loan-form').onsubmit = async e => {
        e.preventDefault();
        const fd = Object.fromEntries(new FormData(e.target));
        const btn = e.target.querySelector('button');
        setButtonLoading(btn, true);
        try {
            const body = {
                originalAmount: +fd.originalAmount,
                amount: +fd.originalAmount,
                interestRate: +fd.interestRate,
                durationMonths: +fd.durationMonths,
                startDate: new Date(fd.startDate).toISOString(),
                status: +fd.status
            };
            await api('POST', `/api/teller/customers/${fd.customerId}/loans`, body);
            toast('New loan issued to customer portfolio');
            e.target.reset();
        } catch (er) {
            toast(er.message, 'error');
        } finally {
            setButtonLoading(btn, false);
        }
    };
}

// --- Customer: Profile & Image upload ---
async function renderCustomerProfile() {
    const m = $main(), uid = getUserId();
    m.innerHTML = '<div class="spinner"></div>';
    try {
        const c = await api('GET', `/api/customer/${uid}/profile`);
        m.innerHTML = `
          <h2 style="margin-bottom:20px" data-aos="fade-down">My Portal Profile</h2>
          
          <div class="card" data-aos="fade-up">
            <div style="display:flex;align-items:center;gap:24px;margin-bottom:30px">
              ${renderAvatar(c.photoUrl, c.name, 'sidebar-user-avatar')}
              <div>
                <h3 style="margin-bottom:4px;font-size:1.4rem">${c.name || ''}</h3>
                <p style="color:var(--text-muted);font-size:0.9rem">User ID: <span style="font-family:monospace">${c.id}</span></p>
              </div>
            </div>
            <dl class="info-card">
              <dt>Primary Email</dt><dd>${c.email || ''}</dd>
              <dt>Mobile Connection</dt><dd>${c.phoneNumber || '—'}</dd>
              <dt>City / State</dt><dd>${c.city || '—'}, ${c.state || '—'}</dd>
              <dt>Address Street</dt><dd>${c.street || '—'}</dd>
              <dt>Postal Code</dt><dd>${c.postalCode || '—'}</dd>
            </dl>
            <button class="btn btn-primary" onclick="openEditProfile()"><i class="fa fa-user-pen"></i> Update Info</button>
          </div>
          
          <div class="card" style="margin-top:20px" data-aos="fade-up" data-aos-delay="100">
            <h3 style="margin-bottom:20px">Change Secure Password</h3>
            <form id="cp-form">
              <div class="form-row">
                ${fi('Current Portal Password', 'currentPassword', 'password')}
                ${fi('New Access Password', 'newPassword', 'password')}
              </div>
              <button class="btn btn-primary" type="submit">Commit Password Change</button>
            </form>
          </div>
        `;
        bindPasswordToggles(document.getElementById('cp-form'));
        document.getElementById('cp-form').onsubmit = async e => {
            e.preventDefault();
            const fd = Object.fromEntries(new FormData(e.target));
            const btn = e.target.querySelector('button');
            setButtonLoading(btn, true);
            try {
                await api('PUT', `/api/customer/${uid}/change-password`, fd);
                toast('Password changed successfully');
                e.target.reset();
            } catch (er) {
                toast(er.message, 'error');
            } finally {
                setButtonLoading(btn, false);
            }
        };
    } catch (e) {
        m.innerHTML = `<p class="error">${e.message}</p>`;
    }
}

async function openEditProfile() {
    const uid = getUserId();
    try {
        const c = await api('GET', `/api/customer/${uid}/profile`);
        openModal('Edit Profile Information', `
          <div class="image-upload-wrapper">
            <div class="image-upload-preview" id="modal-preview-avatar">
              ${c.photoUrl ? `<img src="${c.photoUrl}" style="width:100%;height:100%;object-fit:cover;border-radius:50%">` : '<i class="fa fa-circle-user" style="font-size:2.2rem"></i>'}
            </div>
            <div class="image-upload-btn-container">
              <label>Avatar Photo</label>
              <input type="file" id="modal-photo-file" class="image-upload-input" accept="image/*">
              <label for="modal-photo-file" class="image-upload-trigger">
                <i class="fa fa-cloud-arrow-up"></i> Upload Photo
              </label>
              <span class="image-upload-help">JPG, PNG, GIF or WebP. Max 2MB.</span>
              <input type="hidden" name="photoUrl" id="modal-photo-url" value="${c.photoUrl || ''}">
            </div>
          </div>

          <div class="form-row">
            ${fi('Display Name', 'name', 'text', c.name || '')}
            ${fi('Email Address', 'email', 'email', c.email || '')}
          </div>
          ${fi('Mobile Number', 'phoneNumber', 'text', c.phoneNumber || '')}
          <div class="form-row">
            ${fi('City', 'city', 'text', c.city || '')}
            ${fi('Street Address', 'street', 'text', c.street || '')}
          </div>
          <div class="form-row">
            ${fi('State', 'state', 'text', c.state || '')}
            ${fi('Postal Code', 'postalCode', 'number', c.postalCode || 0)}
          </div>
        `, async fd => {
            fd.postalCode = parseInt(fd.postalCode) || 0;
            const updated = await api('PUT', `/api/customer/${uid}/profile`, fd);
            toast('Profile details saved');
            
            // Sync current sidebar data
            localStorage.setItem('bank_photo_url', updated.photoUrl || '');
            localStorage.setItem('bank_user_name', updated.name || '');
            
            router('Customer'); // reload structure
        });
    } catch (e) {
        toast(e.message, 'error');
    }
}

// --- Customer: Accounts with Premium 3D Flip Card Rendering ---
async function renderCustomerAccounts(page = 1) {
    const m = $main(), uid = getUserId();
    m.innerHTML = `
      <div class="section-header" data-aos="fade-down">
        <h2>My Bank Accounts & Cards</h2>
        <div id="self-create-action"></div>
      </div>
      <div id="al" class="table-container" data-aos="fade-up">${tableLoader(4)}</div>
      
      <!-- Premium 3D flip card details zone -->
      <div id="acct-detail"></div>
    `;

    try {
        const d = await api('GET', `/api/customer/${uid}/accounts`, null, { page, pageSize: 50 });
        
        // Dynamically toggle and inject the "Create Account" button if rules permit it
        // A customer can have Saving (1), Current (2), or both
        const currentTypes = d.map(a => parseInt(a.accountType));
        const hasSaving = currentTypes.includes(1);
        const hasCurrent = currentTypes.includes(2);
        
        const actionEl = document.getElementById('self-create-action');
        if (hasSaving && hasCurrent) {
            actionEl.innerHTML = `<span style="font-size:0.85rem;color:var(--text-muted);font-weight:700;text-transform:uppercase;background:rgba(255,255,255,0.03);padding:8px 16px;border:1px solid var(--border-color);border-radius:var(--radius-full)"><i class="fa fa-check-circle" style="color:#10b981"></i> All Account Types Issued</span>`;
        } else {
            const allowedOptions = [];
            if (!hasSaving) allowedOptions.push(['1', 'Open Savings Account']);
            if (!hasCurrent) allowedOptions.push(['2', 'Open Current Account']);
            
            actionEl.innerHTML = `<button class="btn btn-primary" onclick="triggerCustomerSelfCreate(${JSON.stringify(allowedOptions).replace(/"/g, '&quot;')})"><i class="fa fa-wallet"></i> Open New Account</button>`;
        }

        document.getElementById('al').innerHTML = `
          <table class="data-table">
            <thead>
              <tr>
                <th>Account ID</th>
                <th>Account Type</th>
                <th>Ledger Balance</th>
                <th>Status</th>
                <th>Card Reference</th>
                <th>Actions</th>
              </tr>
            </thead>
            <tbody>
              ${d.length ? d.map(a => `
                <tr>
                  <td>#${a.id}</td>
                  <td style="font-weight:700;color:#ffffff">${acctTypes[a.accountType] || a.accountType}</td>
                  <td style="font-weight:800;color:#10b981">${fmt(a.balance)}</td>
                  <td>
                    <span class="status-badge status-${(a.accountStatus === 1 ? 'active' : a.accountStatus === 2 ? 'frozen' : 'closed')}">
                      ${a.accountStatus === 1 ? 'Active' : a.accountStatus === 2 ? 'Frozen' : 'Closed'}
                    </span>
                  </td>
                  <td>${a.card ? `<i class="fa fa-credit-card"></i> ${a.card.cardType} **** ${a.card.cardNumber.slice(-4)}` : '—'}</td>
                  <td>
                    <button class="btn btn-sm btn-accent" onclick="viewAcctDetail(${a.id})">
                      <i class="fa fa-credit-card"></i> Details & Card
                    </button>
                  </td>
                </tr>
              `).join('') : '<tr><td colspan="6" style="text-align:center">No accounts found. Open your first account to begin.</td></tr>'}
            </tbody>
          </table>
        `;
    } catch (e) {
        document.getElementById('al').innerHTML = `<p class="error">${e.message}</p>`;
    }
}

// Self creation with automatic card mapping
function triggerCustomerSelfCreate(options) {
    const uid = getUserId();
    openModal('Open Premium Bank Account', fsel('Account Type Options', 'accountType', options), async fd => {
        fd.accountType = parseInt(fd.accountType);
        try {
            await api('POST', `/api/customer/${uid}/accounts`, fd);
            toast('Account opened and debit card minted successfully!');
            renderCustomerAccounts(1);
        } catch (err) {
            toast(err.message, 'error');
        }
    });
}

// Render dynamic card detail and flip-scene
async function viewAcctDetail(aid) {
    const det = document.getElementById('acct-detail');
    det.innerHTML = '<div class="spinner"></div>';
    
    try {
        const uid = getUserId();
        const accts = await api('GET', `/api/customer/${uid}/accounts`, null, { page: 1, pageSize: 50 });
        const a = accts.find(x => x.id === aid);
        
        if (!a) throw new Error('Account details unavailable');
        
        const balRes = await api('GET', `/api/customer/accounts/${aid}/balance`);
        const card = a.card;
        
        let cardHTML = '';
        if (card) {
            const cardNetworkIcon = card.cardType.toLowerCase() === 'mastercard' ? 'fa-brands fa-cc-mastercard' : 'fa-brands fa-cc-visa';
            const cardColorGradient = card.cardType.toLowerCase() === 'mastercard' ? 'var(--gold-gradient)' : 'var(--primary-gradient)';
            
            cardHTML = `
              <div class="card-scene" onclick="toggleCardFlip(this)" data-aos="zoom-in">
                <div class="flip-card-inner">
                  <!-- Front Face -->
                  <div class="card-face card-front" style="background:${cardColorGradient}">
                    <div class="card-network-container">
                      <span class="card-bank-brand"><i class="fa fa-building-columns"></i> BankSystem</span>
                      <span class="card-network-logo"><i class="${cardNetworkIcon}"></i></span>
                    </div>
                    <div class="card-chip-container">
                      <div class="card-chip"></div>
                      <div class="card-contactless"><i class="fa fa-rss"></i></div>
                    </div>
                    <div class="card-number-display">${card.cardNumber}</div>
                    <div class="card-meta-row">
                      <div class="card-holder-container">
                        <span class="card-label">Card Holder</span>
                        <span class="card-holder-name">${card.cardHolderName || currentUserData.name}</span>
                      </div>
                      <div class="card-expiry-container">
                        <span class="card-label">Expires</span>
                        <span class="card-expiry-val">${card.expiryDate}</span>
                      </div>
                    </div>
                  </div>
                  <!-- Back Face -->
                  <div class="card-face card-back">
                    <div class="card-magnetic-strip"></div>
                    <div class="card-signature-container">
                      <div class="card-signature-area">${card.iban || 'IBAN NOT ASSIGNED'}</div>
                      <div class="card-cvv-box">${card.cvv}</div>
                    </div>
                    <p class="card-back-info">
                      This card is issued by BankSystem. Use subject to account terms. If found, please return to any institutional branch office. Protected by AES-256 standards.
                    </p>
                  </div>
                </div>
              </div>
              <p style="text-align:center; font-size:0.8rem; color:var(--text-muted); margin-top:10px">
                <i class="fa fa-rotate-left"></i> Click card to view back / CVV security signature
              </p>
            `;
        } else {
            cardHTML = `<div class="error">No bank card is associated with this account.</div>`;
        }

        det.innerHTML = `
          <div class="card" style="margin-top:24px" data-aos="fade-up">
            <div style="display:flex;justify-content:space-between;align-items:center;margin-bottom:24px;flex-wrap:wrap;gap:12px">
              <div>
                <h3 style="margin-bottom:4px">Account Details Dashboard</h3>
                <p style="color:var(--text-muted);font-size:0.9rem">Type: <strong>${acctTypes[a.accountType] || a.accountType}</strong> | Status: <strong>Active</strong></p>
              </div>
              <div class="stat-card" style="padding:12px 24px;border-radius:var(--radius-md);margin-bottom:0">
                <div class="stat-label" style="margin-top:0">Available Balance</div>
                <div class="stat-value" style="font-size:1.6rem;color:#10b981;min-height:unset">${fmt(balRes.balance)}</div>
              </div>
            </div>
            
            <div class="form-row" style="align-items: center; margin-bottom:30px">
              <div>
                ${cardHTML}
              </div>
              <div style="display:flex;flex-direction:column;gap:12px">
                <h4 style="color:#ffffff;border-bottom:1px solid var(--border-color);padding-bottom:8px">Vault Reference Details</h4>
                <p style="font-size:0.9rem;color:var(--text-muted)">IBAN/REF: <span style="font-family:monospace;color:#ffffff">${card?.iban || '—'}</span></p>
                <p style="font-size:0.9rem;color:var(--text-muted)">Card Scheme: <strong style="color:#ffffff">${card?.cardType || 'None'}</strong></p>
                <p style="font-size:0.9rem;color:var(--text-muted)">Routing Reference: <span style="font-family:monospace;color:#ffffff">BKNKUS33AXX</span></p>
                <p style="font-size:0.9rem;color:var(--text-muted)">Created On: <strong style="color:#ffffff">${fmtDate(a.createdDate)}</strong></p>
              </div>
            </div>

            <h3 style="margin-bottom:16px">Account Ledger Transactions</h3>
            <div id="atx" class="table-container">${tableLoader()}</div>
          </div>
        `;
        
        loadAcctTxns(aid, 1);
        AOS.refresh();
    } catch (e) {
        det.innerHTML = `<p class="error">${e.message}</p>`;
    }
}

function toggleCardFlip(cardScene) {
    const cardInner = cardScene.querySelector('.flip-card-inner');
    cardInner.classList.toggle('flipped');
}

async function loadAcctTxns(aid, page = 1) {
    const c = document.getElementById('atx');
    if (!c) return;
    c.innerHTML = tableLoader();
    try {
        const d = await api('GET', `/api/customer/accounts/${aid}/transactions`, null, { page, pageSize: PS });
        c.innerHTML = `
          <table class="data-table">
            <thead><tr><th>TXN #</th><th>Date</th><th>Type</th><th>Amount</th><th>Counterpart</th><th>Note</th></tr></thead>
            <tbody>
              ${d.length ? d.map(t => `
                <tr>
                  <td style="font-family:monospace;color:var(--text-muted)">#${t.id || '—'}</td>
                  <td>${fmtDate(t.date)}</td>
                  <td>
                    <span style="font-weight:700;color:${t.type === 2 ? '#ef4444' : t.type === 3 ? '#10b981' : 'var(--primary)'}">
                      ${txnTypes[t.type] || t.type}
                    </span>
                  </td>
                  <td style="font-weight:800;color:${t.type === 2 ? '#ef4444' : '#10b981'}">${t.type === 2 ? '-' : '+'}${fmt(t.amount)}</td>
                  <td>${t.targetAccountId ? '<i class="fa fa-right-left" style="font-size:0.75rem"></i> #'+t.targetAccountId : '—'}</td>
                  <td>${t.message || '—'}</td>
                </tr>`).join('') : '<tr><td colspan="6">No transaction history.</td></tr>'}
            </tbody>
          </table>
          <div class="pagination">
            <button class="btn btn-sm btn-secondary" ${page <= 1 ? 'disabled' : ''} onclick="loadAcctTxns(${aid}, ${page - 1})">← Previous</button>
            <span class="page-info">Page ${page}</span>
            <button class="btn btn-sm btn-secondary" ${d.length < PS ? 'disabled' : ''} onclick="loadAcctTxns(${aid}, ${page + 1})">Next →</button>
          </div>
        `;
    } catch (e) {
        c.innerHTML = `<p class="error">${e.message}</p>`;
    }
}

// --- Customer: Transfer ---
async function renderCustomerTransfer() {
    const m = $main(), uid = getUserId();
    m.innerHTML = '<div class="spinner"></div>';
    try {
        const accts = await api('GET', `/api/customer/${uid}/accounts`, null, { page: 1, pageSize: 100 });
        m.innerHTML = `
          <h2 style="margin-bottom:20px" data-aos="fade-down">Transfer Assets</h2>
          <div class="card" data-aos="fade-up">
            <form id="tf-form">
              ${fsel('Select Source Ledger', 'src', accts.map(a => [a.id, `#${a.id} — ${acctTypes[a.accountType] || ''} (${fmt(a.balance)})`]))}
              ${fi('Destination Bank Account ID', 'tgt', 'number')}
              ${fi('Transfer Asset Amount', 'amt', 'number')}
              ${fi('Reference Note', 'msg', 'text', '', false)}
              <button class="btn btn-primary" type="submit"><i class="fa fa-paper-plane"></i> Execute Transfer</button>
            </form>
          </div>
          <div id="tf-result"></div>
        `;
        document.getElementById('tf-form').onsubmit = async e => {
            e.preventDefault();
            const fd = Object.fromEntries(new FormData(e.target));
            const amt = parseFloat(fd.amt);

            // Frontend validation
            if (+fd.src === +fd.tgt) {
                return toast('You cannot transfer to the same account.', 'error');
            }
            if (!amt || amt <= 0) {
                return toast('Transfer amount must be greater than zero.', 'error');
            }
            if (!fd.tgt || isNaN(+fd.tgt) || +fd.tgt <= 0) {
                return toast('Please enter a valid destination Account ID.', 'error');
            }

            const btn = e.target.querySelector('button');
            setButtonLoading(btn, true);
            try {
                const r = await api('POST', '/api/customer/transfer', {
                    accountId: +fd.src,
                    targetAccountId: +fd.tgt,
                    amount: amt,
                    message: fd.msg || '',
                    type: 1,
                    date: new Date().toISOString()
                });
                toast(`Transfer completed — TXN #${r.id} — ${fmt(amt)} sent to #${fd.tgt}`);
                document.getElementById('tf-result').innerHTML = `
                  <div class="card" style="margin-top:20px;border-left:4px solid #10b981;box-shadow:0 0 20px rgba(16,185,129,0.05)" data-aos="zoom-in">
                    <h3 style="color:#10b981"><i class="fa fa-circle-check"></i> Transfer Complete</h3>
                    <dl class="info-card">
                      <dt>Transaction ID</dt><dd style="font-family:monospace;color:var(--primary)">#${r.id}</dd>
                      <dt>Settled Amount</dt><dd style="color:#10b981;font-weight:800">${fmt(r.amount)}</dd>
                      <dt>Debited Account</dt><dd>#${r.accountId}</dd>
                      <dt>Credited Account</dt><dd>#${r.targetAccountId || fd.tgt}</dd>
                      <dt>Valuation Date</dt><dd>${fmtDate(r.date)}</dd>
                      <dt>Reference</dt><dd>${fd.msg || '—'}</dd>
                    </dl>
                  </div>`;
                e.target.reset();
                AOS.refresh();
                // refresh accounts list to show updated balances
                renderCustomerAccounts(1);
            } catch (er) {
                toast(er.message, 'error');
            } finally {
                setButtonLoading(btn, false);
            }
        };
    } catch (e) {
        m.innerHTML = `<p class="error">${e.message}</p>`;
    }
}

// --- Customer: Loans ---
async function renderCustomerLoans() {
    const m = $main(), uid = getUserId();
    m.innerHTML = `
      <h2 style="margin-bottom:20px" data-aos="fade-down">My Loan Portfolios</h2>
      <div id="ll" class="table-container" data-aos="fade-up">${tableLoader(6)}</div>
    `;
    try {
        const d = await api('GET', `/api/customer/${uid}/loans`);
        document.getElementById('ll').innerHTML = `
          <table class="data-table">
            <thead>
              <tr>
                <th>Loan ID</th>
                <th>Principal Amount</th>
                <th>Remaining Liability</th>
                <th>Interest Rate</th>
                <th>Loan Term</th>
                <th>Status</th>
                <th>Creation Date</th>
              </tr>
            </thead>
            <tbody>
              ${d.length ? d.map(l => `
                <tr style="cursor:pointer" onclick="viewLoan(${l.id})">
                  <td>#${l.id}</td>
                  <td>${fmt(l.originalAmount)}</td>
                  <td style="font-weight:700;color:#ef4444">${fmt(l.amount)}</td>
                  <td>${l.interestRate}%</td>
                  <td>${l.durationMonths} months</td>
                  <td>
                    <span class="status-badge status-${l.status === 2 ? 'active' : l.status === 3 ? 'closed' : 'suspended'}">
                      ${loanStatuses[l.status] || l.status}
                    </span>
                  </td>
                  <td>${fmtDate(l.startDate)}</td>
                </tr>`).join('') : '<tr><td colspan="7" style="text-align:center">No loan records associated with this customer profile.</td></tr>'}
            </tbody>
          </table>
        `;
    } catch (e) {
        document.getElementById('ll').innerHTML = `<p class="error">${e.message}</p>`;
    }
}

async function viewLoan(id) {
    try {
        const l = await api('GET', `/api/customer/loans/${id}`);
        openModal('Loan Portfolio Information', `
          <dl class="info-card">
            <dt>Loan Account Ref</dt><dd>#${l.id}</dd>
            <dt>Principal Sum</dt><dd>${fmt(l.originalAmount)}</dd>
            <dt>Remaining Balance</dt><dd style="color:#ef4444;font-weight:800">${fmt(l.amount)}</dd>
            <dt>Annual Interest Rate</dt><dd>${l.interestRate}%</dd>
            <dt>Amortization Term</dt><dd>${l.durationMonths} months</dd>
            <dt>Order Status</dt><dd>${loanStatuses[l.status] || l.status}</dd>
            <dt>Effective Date</dt><dd>${fmtDate(l.startDate)}</dd>
          </dl>
        `, () => { closeModal(); });
    } catch (e) {
        toast(e.message, 'error');
    }
}

// --- Init Event triggers ---
document.addEventListener('DOMContentLoaded', () => {
    if (typeof AOS !== 'undefined') {
        AOS.init({ duration: 800, once: true, disable: 'mobile' });
    }
    if (getToken()) router(getRole());
    else router('login');
});

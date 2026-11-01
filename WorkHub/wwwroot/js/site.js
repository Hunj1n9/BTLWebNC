// ===== Hàm dùng chung cho AJAX =====

// Lấy token chống CSRF (được in ra trong _Layout bằng @Html.AntiForgeryToken())
function getToken() {
    var input = document.querySelector('input[name="__RequestVerificationToken"]');
    return input ? input.value : '';
}

// Gửi POST bằng AJAX, data là object { key: value }, trả về JSON
async function ajaxPost(url, data) {
    var body = new URLSearchParams();
    for (var key in data) {
        if (data[key] !== null && data[key] !== undefined) body.append(key, data[key]);
    }
    var res = await fetch(url, {
        method: 'POST',
        headers: {
            'RequestVerificationToken': getToken(),
            'X-Requested-With': 'XMLHttpRequest'
        },
        body: body
    });
    if (!res.ok) throw new Error('Lỗi máy chủ: ' + res.status);
    return res.json();
}

// Gửi GET bằng AJAX, trả về text (HTML của Partial View) hoặc JSON
async function ajaxGet(url, asJson) {
    var res = await fetch(url, { headers: { 'X-Requested-With': 'XMLHttpRequest' } });
    if (!res.ok) throw new Error('Lỗi máy chủ: ' + res.status);
    return asJson ? res.json() : res.text();
}

// Chờ người dùng ngừng gõ rồi mới gọi hàm (dùng cho ô tìm kiếm)
function debounce(fn, ms) {
    var timer;
    return function () {
        var args = arguments;
        clearTimeout(timer);
        timer = setTimeout(function () { fn.apply(null, args); }, ms);
    };
}

// ===== Menu sidebar trên điện thoại =====
document.addEventListener('DOMContentLoaded', function () {
    var toggle = document.getElementById('sidebarToggle');
    var sidebar = document.getElementById('sidebar');
    if (toggle && sidebar) {
        toggle.addEventListener('click', function () {
            sidebar.classList.toggle('open');
        });
    }
});

// ===== Thông báo (quả chuông) =====
async function loadNotifications() {
    var list = document.getElementById('notifList');
    if (!list) return;

    try {
        var data = await ajaxGet('/Notification/GetUnread', true);

        var badge = document.getElementById('bellCount');
        badge.textContent = data.soChuaDoc;
        badge.classList.toggle('d-none', data.soChuaDoc === 0);

        list.innerHTML = '';
        if (data.items.length === 0) {
            list.innerHTML = '<div class="px-3 py-2 text-muted small">Chưa có thông báo</div>';
            return;
        }

        data.items.forEach(function (tb) {
            var a = document.createElement('a');
            a.className = 'notif-item' + (tb.daDoc ? '' : ' unread');
            a.href = tb.lienKet || '#';
            a.textContent = tb.noiDung; // dùng textContent để chống XSS

            var time = document.createElement('small');
            time.textContent = tb.thoiGian;
            a.appendChild(time);

            // Bấm vào thì đánh dấu đã đọc rồi chuyển trang
            a.addEventListener('click', async function (e) {
                e.preventDefault();
                if (!tb.daDoc) await ajaxPost('/Notification/MarkAsRead', { maThongBao: tb.maThongBao });
                window.location.href = a.getAttribute('href');
            });
            list.appendChild(a);
        });
    } catch (err) {
        console.log(err);
    }
}

document.addEventListener('DOMContentLoaded', function () {
    if (!document.getElementById('bellBtn')) return;

    loadNotifications();
    setInterval(loadNotifications, 20000); // tự cập nhật mỗi 20 giây

    document.getElementById('markAllRead').addEventListener('click', async function (e) {
        e.preventDefault();
        await ajaxPost('/Notification/MarkAllAsRead', {});
        loadNotifications();
    });
});

// ===== Khung bình luận / chat (dùng chung cho Công việc và Nhóm) =====
// options: { box, form, input, query: { maCongViec } hoặc { maNhom } }
function initChat(options) {
    var box = document.getElementById(options.box);
    var form = document.getElementById(options.form);
    var input = document.getElementById(options.input);
    var shownIds = {};

    function addMessage(m) {
        if (shownIds[m.maBinhLuan]) return;
        shownIds[m.maBinhLuan] = true;

        var div = document.createElement('div');
        div.className = 'chat-msg' + (m.laCuaToi ? ' mine' : '');

        var meta = document.createElement('div');
        meta.className = 'meta';
        meta.textContent = m.nguoiGui + ' · ' + m.thoiGian;

        var text = document.createElement('div');
        text.className = 'text';
        text.textContent = m.noiDung; // textContent: không chạy mã HTML/script người dùng nhập

        div.appendChild(meta);
        div.appendChild(text);
        box.appendChild(div);
    }

    async function load() {
        var url = '/Comment/List?' + new URLSearchParams(options.query).toString();
        var list = await ajaxGet(url, true);
        var atBottom = box.scrollTop + box.clientHeight >= box.scrollHeight - 20;
        if (list.length === 0 && box.children.length === 0) {
            box.innerHTML = '<div class="text-muted small empty">Chưa có tin nhắn nào.</div>';
        }
        list.forEach(function (m) {
            var empty = box.querySelector('.empty');
            if (empty) empty.remove();
            addMessage(m);
        });
        if (atBottom) box.scrollTop = box.scrollHeight;
    }

    form.addEventListener('submit', async function (e) {
        e.preventDefault();
        var noiDung = input.value.trim();
        if (noiDung === '') return; // kiểm tra phía client
        if (noiDung.length > 2000) { alert('Nội dung tối đa 2000 ký tự'); return; }

        var data = Object.assign({ noiDung: noiDung }, options.query);
        var result = await ajaxPost('/Comment/Post', data);
        if (result.success) {
            var empty = box.querySelector('.empty');
            if (empty) empty.remove();
            addMessage(result);
            input.value = '';
            box.scrollTop = box.scrollHeight;
        } else {
            alert(result.message);
        }
    });

    load();
    setInterval(load, 10000); // tải tin mới mỗi 10 giây
}

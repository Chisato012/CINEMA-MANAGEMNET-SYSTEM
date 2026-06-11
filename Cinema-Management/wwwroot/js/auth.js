document.addEventListener('DOMContentLoaded', function () {
    const togglePassword = document.querySelector('#togglePassword');
    const passwordInput = document.querySelector('#passwordInput');

    //Lưu lại 2 mã SVG để chuyển đổi
    const eyeSlashIcon = `<svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" fill="currentColor" viewBox="0 0 16 16"><path d="M13.359 11.238C15.06 9.72 16 8 16 8s-3-5.5-8-5.5a7.028 7.028 0 0 0-2.79.588l.77.771A5.944 5.944 0 0 1 8 3.5c2.12 0 3.879 1.168 5.168 2.457A13.134 13.134 0 0 1 14.828 8c-.058.087-.122.183-.195.288-.335.48-.83 1.12-1.465 1.755l.192.195z"/><path d="M11.297 9.176a3.5 3.5 0 0 0-4.474-4.474l.823.823a2.5 2.5 0 0 1 2.829 2.829l.822.822zm-2.943 1.299.822.822a3.5 3.5 0 0 1-4.474-4.474l.823.823a2.5 2.5 0 0 0 2.829 2.829z"/><path d="M3.35 5.47c-.18.16-.353.322-.518.487A13.134 13.134 0 0 0 1.172 8l.195.288c.335.48.83 1.12 1.465 1.755C4.121 11.332 5.881 12.5 8 12.5c.716 0 1.39-.133 2.02-.36l.77.772A7.029 7.029 0 0 1 8 13.5C3 13.5 0 8 0 8s.939-1.721 2.641-3.238l.708.709zm10.296 8.884-12-12 .708-.708 12 12-.708.708z"/></svg>`;
    const eyeIcon = `<svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" fill="currentColor" viewBox="0 0 16 16"><path d="M10.5 8a2.5 2.5 0 1 1-5 0 2.5 2.5 0 0 1 5 0z"/><path d="M0 8s3-5.5 8-5.5S16 8 16 8s-3 5.5-8 5.5S0 8 0 8zm8 3.5a3.5 3.5 0 1 0 0-7 3.5 3.5 0 0 0 0 7z"/></svg>`;

    togglePassword.addEventListener('click', function () {
        const isPassword = passwordInput.getAttribute('type') === 'password';

        if (isPassword) {
            //chuyển sang text để xem mật khẩu
            passwordInput.setAttribute('type', 'text');
            this.innerHTML = eyeIcon; //đổi icon sang con mắt mở
        } else {
            passwordInput.setAttribute('type', 'password'); //chuyển lại thành password
            this.innerHTML = eyeSlashIcon; //đổi icon sang con mắt nhắm lại
        }
    });

    const emailInput = document.getElementById("email-input")
    const emailError = document.getElementById("email-error")

    //Hàm check email
    function validateEmail() {
        const value = emailInput.value.trim();

        const atCount = (value.match(/@/g) || []).length; //Hàm đếm @

        if (value === "") {
            emailError.textContent = "";
            return false;
        }

        if (atCount === 0) {
            emailError.textContent = "Email phải chứa ký tự @";
            return false;
        }
        // @* Kiểm tra 2 dấu @ * @
        if (atCount > 1) {
            emailError.textContent = "Email chỉ được chứa một ký tự @";
            return false;
        }

        const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

        if (!emailRegex.test(value)) {
            emailError.textContent = "Email không đúng định dạng";
            return false;
        }

        emailError.textContent = "";
        return true;
    }
    //lắng nghe sự kiện
    emailInput.addEventListener("input", validateEmail);

    const form = document.querySelector("form");

    form.addEventListener("submit", function (e) {

        if (!validateEmail()) {
            e.preventDefault();
            emailInput.focus();
        }
    });

    //HÀM CHECK SỐ ĐIỆN THOẠI 
    const phoneInput = document.getElementById("phone-input");
    const phoneError = document.getElementById("phone-error");

    function validatePhone() {

        const value = phoneInput.value.trim();

        if (value === "") {
            phoneError.textContent = "";
            return false;
        }

        const phoneRegex = /^0\d{9}$/;

        if (!phoneRegex.test(value)) {
            phoneError.textContent =
                "Số điện thoại phải gồm 10 chữ số và bắt đầu bằng 0";
            return false;
        }

        phoneError.textContent = "";
        return true;
    }

    phoneInput.addEventListener(
        "input",
        validatePhone
    );
});

//PHẦN CHECK NHẬP LẠI MẬT KHẨU
document.addEventListener('DOMContentLoaded', function(){
    const passwordInput =
    document.getElementById("passwordInput");

    const confirmPasswordInput =
    document.getElementById("confirmPasswordInput");

    const confirmPasswordError =
    document.getElementById("confirmPasswordError");

    function validateConfirmPassword() {

        const password = passwordInput.value;

        const confirmPassword = confirmPasswordInput.value;

        if (confirmPassword === "") {
            confirmPasswordError.textContent = "";
            return false;
        }

        if (password !== confirmPassword) {
            confirmPasswordError.textContent = "Mật khẩu xác nhận không khớp";
            return false;
        }

        confirmPasswordError.textContent = "";
        return true;
    }

    confirmPasswordInput.addEventListener(
        "input",
        validateConfirmPassword
    );

    //Phần check mật khẩu có đủ ký tự đặc biệt
    const passwordError = document.getElementById("password-error");

    function validatePassword() {
        const value = passwordInput.value;

        // Nếu rỗng thì xóa lỗi (trình duyệt sẽ tự báo lỗi vì đã có thuộc tính required trong HTML)
        if (value === "") {
            passwordError.textContent = "";
            return false;
        }

        // Biểu thức Regex: 1 chữ hoa, 1 số, 1 ký tự đặc biệt và độ dài tối thiểu 8 ký tự
        const passwordRegex = /^(?=.*[A-Z])(?=.*\d)(?=.*[\W_]).{8,}$/;

        if (!passwordRegex.test(value)) {
            passwordError.textContent = "Mật khẩu cần ít nhất 8 ký tự, gồm 1 chữ hoa, 1 số và 1 ký tự đặc biệt";
            return false;
        }

        passwordError.textContent = "";
        return true;
    }

    // Lắng nghe sự kiện khi người dùng gõ phím
    passwordInput.addEventListener("input", validatePassword);

    //Phần check tên k được có số
    const nameError = document.getElementById("name-error");
    const nameInput = document.getElementById("name-input")
    function validateName(){
        const value = nameInput.value;

        if(value === "" ){
            nameError.textContent = "";
            return false
        }

        //Regex
        const regex = /^[\p{L}\s]+$/u;
        if(!regex.test(value)){
            nameError.textContent = "Tên không được có số"
            return false
        }

        nameError.textContent = "";
        return true;
    }
    nameInput.addEventListener("input", validateName)

});
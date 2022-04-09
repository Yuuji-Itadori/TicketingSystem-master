const changeLogin = document.querySelector('#ChangeLogin');
const buttons = document.querySelectorAll('#ChangeLogin button');
buttons.forEach(button => {
    button.addEventListener("click", () => {
        changeLogin.classList.toggle('RightSlide');
        changeLogin.classList.toggle('LeftSlide');
    });
});

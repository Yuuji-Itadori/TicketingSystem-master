const checkBox = document.querySelector("#darkMode");
const themeLinker = document.querySelector("#theme");

let isDarkMode = localStorage.getItem('darkMode');
if(isDarkMode == 'true'){
    checkBox.checked = true;
    setTheme("darkMode");
}

checkBox.addEventListener("change", () => {
    if(checkBox.checked){
        setTheme("darkMode");
        localStorage.setItem('darkMode', 'true');
    }
    else {
        setTheme(null);
        localStorage.setItem('darkMode', 'false');
    }
});
function setTheme(sheet) {
    themeLinker.href = sheet == null ? "" 
        : "/css/" + sheet + ".css";
}

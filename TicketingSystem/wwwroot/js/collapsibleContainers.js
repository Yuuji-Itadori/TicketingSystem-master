const allSections = document.querySelectorAll('.collapsibleContainer');
let currentlyActive = document.querySelector('.collapsibleContainer.active');
let animationInProgress = false;

allSections.forEach(section => {
    section.addEventListener("click", () => {
        if (currentlyActive === section) return;
        if (animationInProgress) return;
        switchSelection(section);
    });
});

function switchSelection(section) {
    // Changing the currently Active section
    currentlyActive.classList.remove("active");
    section.classList.add("active");
    currentlyActive = section;
    animationInProgress = true;
    setTimeout(() => { animationInProgress = false }, 1000);
}
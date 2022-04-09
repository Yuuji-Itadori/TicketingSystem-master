const messageBox = document.querySelector('#messageSlider');
const messageWidth = '20.33vw';
let currentMessage = 0;
setInterval(function(){
    currentMessage++;
    if(currentMessage > govtMessages.length - 2) currentMessage = 0;
    messageBox.style.transform = "translateX(-"+ currentMessage * messageWidth +"px)";
}, 7000);

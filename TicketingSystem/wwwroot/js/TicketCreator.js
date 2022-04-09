const details = document.querySelector('#detail');
const summary = document.querySelector('#summary');
const services = document.querySelector('#service');
const severity = document.querySelector('#severity');
const username = document.querySelector('#username');
const locations = document.querySelector('#location');
const ticketType = document.querySelector('#ticketType');
const locationSelected = document.querySelector('#SelectID');
const detailsLabel = document.querySelector('#detail label');
const summaryLabel = document.querySelector('#summary label');
const severityLable = document.querySelector('#severity label');
const locationLable = document.querySelector('#location label');

ticketType.addEventListener("change", () => { ticketChanged(ticketType.value); });
ticketChanged(ticketType.value);

function ticketChanged(ticketType) {
    switch (ticketType) {
        // Password Reset
        case '0':
            setVisibility(summary, false);
            setVisibility(details, false);
            setVisibility(username, true);
            setVisibility(services, true);
            setVisibility(severity, false);
            setVisibility(locations, false);
            break;
        // Access Control
        case '1':
            setVisibility(summary, true);
            setVisibility(details, true);
            setVisibility(username, true);
            setVisibility(services, true);
            setVisibility(severity, false);
            setVisibility(locations, false);
            setLabelName(detailsLabel, "Reason");
            setLabelName(summaryLabel, "Permission");
            break;
        // Procurement
        case '2':
            setVisibility(summary, true);
            setVisibility(details, true);
            setVisibility(severity, false);
            setVisibility(username, false);
            setVisibility(services, false);
            setVisibility(locations, true);
            setLabelName(summaryLabel, "Requested Item");
            setLabelName(detailsLabel, "Reason / Details");
            break;
        // Standard Services
        case '3':
            setVisibility(summary, true);
            setVisibility(details, true);
            setVisibility(services, true);
            setVisibility(username, false);
            setVisibility(severity, false);
            setVisibility(locations, true);
            setLabelName(summaryLabel, "Summary");
            setLabelName(detailsLabel, "Details");
            break;
        // Non-Standard Services
        case '4':
            setVisibility(summary, true);
            setVisibility(details, true);
            setVisibility(services, true);
            setVisibility(severity, false);
            setVisibility(username, false);
            setVisibility(locations, true);
            setLabelName(summaryLabel, "Summary");
            setLabelName(detailsLabel, "Details");
            break;
        // IT Support Services
        case '5':
            setVisibility(summary, true);
            setVisibility(details, true);
            setVisibility(severity, true);
            setVisibility(services, true);
            setVisibility(username, false);
            setVisibility(locations, true);
            setLabelName(summaryLabel, "Summary");
            setLabelName(detailsLabel, "Details");
            setLabelName(severityLable, "Severity");
            break;
    }
}
function setVisibility(inputField, visible) {
    inputField.style.display = !visible ? "none" : "initial";
    return visible;
}
function setLabelName(label, text) {
    label.innerText = text;
}
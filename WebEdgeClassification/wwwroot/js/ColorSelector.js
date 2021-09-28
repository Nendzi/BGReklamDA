//ColorPicker
const picker = new ColorPickerControl({
    container: document.getElementById("colorPickerWindow"),
    theme: 'dark'
});

function openColorPicker() {
    var clrSlcr = document.getElementById("colorPickerWindow");
    clrSlcr.style.display = "block";
}

picker.on('open', (instance) => {
    console.log('Event: "open"', instance);
});
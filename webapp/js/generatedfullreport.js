// make editable
function toggleEditableDoc() {
    document.querySelectorAll('[contenteditable]').forEach(function (el) {
        if (el.getAttribute('contenteditable') == 'false') {
            el.setAttribute('contenteditable', 'true');
        } else {
            el.setAttribute('contenteditable', 'false');
        }
    });
}
function closeWindow(){
    window.close();
}
function printDoc(){
    window.print();
}
// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.
function promptFile(accept = '*/*') {
    return new Promise((resolve, reject) => {
        const input = document.createElement('input');
        input.type = 'file';
        input.accept = accept;
        input.onchange = () => {
            const file = input.files[0];
            const reader = new FileReader();
            if (file) {
                reader.onload = e=>resolve(e.target.result);
                reader.onerror = e => reject(e);
                reader.readAsDataURL(file);
            }
            else reject("No file selected");
        };
        input.click();
    });
}
// Write your JavaScript code.
// document.addEventListener('DOMContentLoaded', ()=>{
//     document.querySelectorAll('.separator').forEach(el=>{
//         for (let i=0; i < el.children.length; i++){
//             if(el.children[i].nodeType === Node.ELEMENT_NODE){
//                 let d=document.createElement('div');
//                 el.insertAdjacentElement(d.)
//             }
//         }
//     })
//    
//    
// })
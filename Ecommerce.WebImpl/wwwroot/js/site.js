// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.
class CartChangedEvent extends CustomEvent{
    static NAME = 'cartChanged';
    constructor(newQuantity, sellerId, productId, added=false){
        super(CartChangedEvent.NAME, {detail:{newQuantity, sellerId, productId,added}});
    }
}
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

/**
 * 
 * @param element : Element
 * @return Element
 */
function cloneWithScript(element){
    const el = element.cloneNode(true);
    const script = el.querySelector('script');
    if(script){
        const news = document.createElement('script');
        news.textContent = script.textContent;
        el.replaceChild(news, script);
    }
    return el;
}

/**
 * 
 * @param name : string
 * @param value
 * @param type
 * @return HTMLInputElement
 */
function createInput(name, value=null, type='hidden'){
    const i = document.createElement('input');
    i.name = name;
    i.value = value;
    i.type = type;
    return i;
}
function CreateOption(data) {
    let html  =`
    <div ${data.propertyId?'data-propertyid=' + data.propertyId:'data-keyname=' + data.customKey} class="w-100 partial-parent product-options py-2 px-1">
    <script>
        ((s)=>{
            document.addEventListener('DOMContentLoaded', load);
            s.parentElement.addEventListener('htmx:load', load);
            function load(e){
                if(e.type ==='htmx:load') e.stopPropagation();
                const parent = s.parentElement;
                parent._selected = s.nextElementSibling.nextElementSibling.querySelector('button[data-option="@(selectedId)"]');
                parent._selected.classList.add('bg-secondary-subtle');
                parent._selected.classList.remove('bg-light');
                parent.querySelectorAll('button').forEach(b=>{
                    b.addEventListener('click', ev=>{
                        ev.preventDefault();
                        ev.stopPropagation();
                        parent.dataset.selected = b.dataset.option;
                        parent.dataset.value = b.dataset.value;
                        parent._selected.classList.remove('bg-secondary-subtle');
                        parent._selected.classList.add('bg-light');
                        parent._selected = b;
                        b.classList.add('bg-secondary-subtle');
                        b.classList.remove('bg-light');
                        parent.dispatchEvent(new Event('change', {bubbles:true, cancelable:true}));
                    })
                })
                parent.querySelectorAll('i').forEach(i=>{
                    i.addEventListener('click', ev=>{
                        ev.preventDefault();
                        ev.stopPropagation();
                        htmx.ajax('POST', '/@(nameof(Product))?handler=deleteOption&OptionId=' + event.currentTarget.dataset.optionid, {
                            target: '#popupResult',
                            swap: 'innerHTML',
                            values: {
                                __RequestVerificationToken: s.nextElementSibling.value,
                            }, headers: {'Content-Type': 'application/x-www-form-urlencoded'}
                        });
                    })
                })
            }
        })(document.currentScript)
        //# sourceURL=/ProductOptions/@(nameof(_ProductOptionPartial)).js
    </script>
    @Html.AntiForgeryToken()
    <div class="d-flex justify-content-start align-items-center align-content-center gap-2">
        <p class="fs-6 fw-bold">Seçenekler:</p>
        <div class="d-flex gap-3">
            @foreach (var option in Model.Options){
                <div class="d-inline-flex gap-1">
                    <button data-value="@option.option.Value" data-option="@option.option.Id" class="btn btn-primary hover-darken hover-grow-small bg-light text-center align-middle border-2 border-secondary-subtle border-opacity-50 text-bg-secondary rounded-4">
                        <p class="fs-5 text-center text-muted">@option.option.Value</p>
                    </button>
                    @if (Model.Editable){
                        <i data-optionid="@option.option.Id" class="bi float-end bi-trash-fill text-danger fs-5 hover-grow" style="cursor:pointer;"></i>
                    }    
                </div>
            }    
        </div>
        
    </div>
</div>
    `
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
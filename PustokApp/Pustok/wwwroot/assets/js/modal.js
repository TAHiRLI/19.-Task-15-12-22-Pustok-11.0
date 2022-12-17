const modalBtns = document.getElementsByClassName("modal-btn");
const modalContent = document.querySelector('.modal-dialog');
for (const btn of modalBtns) {
    btn.addEventListener("click", (e) => {
        e.preventDefault();
        let link = btn.getAttribute("href");
        console.log(link);


        fetch(link)
        .then(response => response.text())
        .then(data=> {
            console.log(data)
            modalContent.innerHTML = data;
         })

    })
}

$(document).on("click", ".add-to-basket", function (e) {
    e.preventDefault();
    let link = $(this).attr("href");
  

    fetch(link)
        .then(response => {
            if (!response.ok) {
                Swal.fire({
                    title: 'Error!',
                    text: 'This product is out of stock',
                    icon: 'error',
                    confirmButtonText: 'Ok'
                })
                throw new Error("product out of stock");
                return;
            }
            return response.text();
        })
        .then(data => {
            $("#BasketPartialHolder").html(data);
        })
        .catch(error=>{
        console.log(error)
             })

    
})
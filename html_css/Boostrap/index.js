function toggle(element){
    var elementString = "#" + element; //get a string that has the id of the element
    if($(elementString).css('display') == 'none'){ //if element is hidden
        $(elementString).show();
    }
    else{
        $(elementString).hide();
    }
}
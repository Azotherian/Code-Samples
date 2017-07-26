function toggle(element){
                var temp = document.getElementById(element);
                if(temp.hidden){
                    var all = document.getElementsByClassName('accordian');
                    for(var i = 0; i < all.length; i++) {
                        all[i].hidden = true;
                    }
                    temp.hidden = false;
                } else {
                    temp.hidden = true;
                }
            }
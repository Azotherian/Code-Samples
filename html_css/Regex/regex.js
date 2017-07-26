function isValidDate(d) {
   var dateRegex = /^(0\d{2}[1-9]|[1-9][0-9]{3})[\-](0[1-9]|1[012])[\-](0[1-9]|[12][0-9]|3[01])$/;
   //return true if d is in correct YYYY-MM-DD format, else return false
   return dateRegex.test(d);
}

function testDate(dateStr, expected) {
   console.log(
         dateStr
      + (expected ? " - accepted - " : " - rejected - ")
      + ((isValidDate(dateStr) === expected) ? "PASS" : "FAIL")
   );
}
function run(){
    testDate("2016-05-20", true);    //Today
    testDate("2016-5-28", false);   //Apocalypse
    testDate("0001-12-25", true);   //first Christmas
    testDate("0000-01-01", false);  //start of our calendar
    testDate("-100-04-01", false);  //April Fool's Day 100 BC
    testDate("2000-02-30", true);   //extra Leap Day to start a millenia
    testDate("40000-01-01", false); //The Emperor protects! Death to heretics! (WH40K references)
    testDate("YYYY-MM-DD", false);  //correct date format, but not an actual date itself
    testDate("dancing with you Saturday night", false); //that kind of a date is an epic fail, mostly because I'm not your type
    testDate("palm tree", false);   //the only plant that produces dates--get it? dates! ha! I slay myself.
}

# A web crawler 

The goal is to have a command line based web crawler to allow crawling a site and find broken links or pages throwing errors.

# Todo

To get a basic version:
- Add a timer to stop crawling after x seconds
- Add a way to log info/debug level stuff instead of using console.writeline
- Make the webcrawler class async (?)

# To Fix

- BadRequest on # urls
	"https://preview.janitv.be/#";BadRequest
	"https://preview.janitv.be/#menu";BadRequest


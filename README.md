
# A web site crawler 

The goal is to have a command line based web crawler to allow crawling a site and find broken links or pages throwing errors.
External links are ignored.

# Todo

To get a basic version:

- Add a way to log info/debug level stuff instead of using console.writeline.
- Avoid downloading images or other binary files.
  -> Check for extensions? Using HEAD alone doesn't seem to cut it on some servers.

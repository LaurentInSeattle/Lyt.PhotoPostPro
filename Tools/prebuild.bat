rem this needs to be run using a start /wait command or else the web service will fail.  
rem exit 0
cd
cd %~p0
cd 
Lyt.Translator.Cli.exe PppLanguages.json
pause
cd 
exit 0

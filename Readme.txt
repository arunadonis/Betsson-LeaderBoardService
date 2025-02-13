Following steps will help you to build and run the code.

Prerequisite
1. VS2022
2. .Net8
3. DockerForWindows

Steps to build:
1. Download the code to your desired location.
2. Open the LeaderBoardService.sln file in Visual Studio 2022
3. Build the solution

Steps to run the service
1. Run docker
2. Run the below command in command window (RabbitMq)
	docker run -p 5672:5672 -p 15672:15672 rabbitmq:management
3. Run the below command in command window (SQL)
	docker run -e "ACCEPT_EULA=Y" -e "MSSQL_SA_PASSWORD=#Welcome123" -p 1433:1433 mcr.microsoft.com/mssql/server:2022-latest
4. Run the DB Migration (below command in Package Manager Console)
	update-database (this command will create required database, tables and seeds the Game table with 2 entries)
5. Run the code from VS2022 and it should open the Swagger page in default browser window.

Follow the below steps to publish messages
1. Run LeaderBoardService.Publisher project and it should open console window with information to feed data and publish message (testing!!!)
	
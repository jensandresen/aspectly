CONFIGURATION=debug
OUTPUT_DIR=${PWD}/output

init: restore build test

clean:
	rm -Rf $(OUTPUT_DIR)
	mkdir $(OUTPUT_DIR)

restore:
	cd src && dotnet restore -v q

build:
	cd src && dotnet build -c $(CONFIGURATION) -v q

test:
	cd src && dotnet test --no-build --no-restore -c $(CONFIGURATION) -v q

pack:
	cd src && dotnet pack \
		--no-build \
		--no-restore \
		-c $(CONFIGURATION) \
		-o $(OUTPUT_DIR) \
		./Aspectly.Core/Aspectly.Core.csproj

release: CONFIGURATION=Release
release: clean restore build test pack
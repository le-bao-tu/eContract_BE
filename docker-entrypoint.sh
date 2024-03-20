#!/bin/bash

# "Before run container with shell script => Go to notepadd++ => Edit => EOL Conversion => Unix (LF)"

echo "Config Env Variable for Backend App"
# Assign the filename
sed -i 's/IS_TESTING/'"${IS_TESTING}"'/g' appsettings.json
sed -i 's/DATABASE_TYPE/'"${DATABASE_TYPE}"'/g' appsettings.json
sed -i 's/DATABASE_USER/'"${DATABASE_USER}"'/g' appsettings.json
sed -i 's/DATABASE_PASSWORD/'"${DATABASE_PASSWORD}"'/g' appsettings.json
sed -i 's/DATABASE_HOST/'"${DATABASE_HOST}"'/g' appsettings.json
sed -i 's/DATABASE_PORT/'"${DATABASE_PORT}"'/g' appsettings.json
sed -i 's/DATABASE_NAME/'"${DATABASE_NAME}"'/g' appsettings.json

echo "Successfully Config !!!"
exec "$@"
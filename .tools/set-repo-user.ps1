param (
    [string]$username
)

git config --local credential.https://github.com.username $username

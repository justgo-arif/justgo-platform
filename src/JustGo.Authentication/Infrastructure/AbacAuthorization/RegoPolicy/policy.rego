package allowCredentialAdd

default allow = false

allow {
    input.action == "Add"
    input.resource=="Credential"
    input.user.role == "NGB admin"
}
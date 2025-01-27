# Harper
A minecraft server manager, through a discord bot.

## Installation
*commands starting with `#` are executed as the root user*
### Debian
1. download the harper-VERSION.deb file from "releases" (latest is strongly suggested)
2. install the debian package using something like:
   ```sh
   # dpkg -i "harper-VERSION.deb"
   ```
3. enable the systemd unit so it starts on startup, supply the `--now` argument, to also start it immediately
    ```sh
    # systemctl enable harper.service --now
    ```
    or just start the unit
    ```sh
    # systemctl start harper.service
    ```
4. after starting the unit some configuration files have been created at `/etc/harper`:
   - `command_perms` contains a value-list of the user-ids and the permission level this user has. where the id 0 applies to all. read more about the options in the file itself.
   - `mcserver_settings.jsonc` contains the settings for the minecraft server(s) you're able to start. There is more in-depth explanation in the file itself.
5. write your bot token as an environment variable to `/etc/harper/token.env`:
   ```sh
    # echo "HARPER_BOT_TOKEN=tokenhere" > /env/harper/token.env
    # chmod 600 /env/harper/token.env
   ```
6. lastly, you can start the harper unit again and check whether it has successfully started
   ```sh
   # systemctl start harper.service
   # systemctl status harper.service
   ```

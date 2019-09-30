# Murg

Murg is a console tool to simplify the process of reorganizing downloaded music. It uses info like file name, count of tracks in a directory to find [Discogs](https://www.discogs.com/) info. It renames a directory and tracks. It sets tags too.

## How to use it

You can install Murg from [snapcraft](https://snapcraft.io/):

```bash
sudo snap install --edge murg
sudo snap connect murg:mount-observe
sudo snap connect murg:process-control
```

Murg is a prototype. I recommend to run it with `--dry` option first.

```bash
murg --dry "Music/攻殻機動隊 STAND ALONE COMPLEX O.S.T"
```

There is `--recursive` option if you want to process subdirectories.

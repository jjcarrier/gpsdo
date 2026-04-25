# Bash completion for gpsdo CLI.
# Load with: source ./completion/gpsdo.bash

_gpsdo_completions() {
    local cur prev
    cur="${COMP_WORDS[COMP_CWORD]}"
    prev="${COMP_WORDS[COMP_CWORD-1]}"

    local options="--enumerate --serial --blink --info --status --on --off --freq1 --f1 --freq2 --f2 --step1 --s1 --step2 --s2 --mode --m --mode1 --m1 --mode2 --m2 --pll --json --interactive --debug"

    case "${prev}" in
        --mode|--m)
            COMPREPLY=( $(compgen -W "NORMAL LOW_POWER" -- "${cur}") )
            return 0
            ;;
        --mode1|--m1)
            COMPREPLY=( $(compgen -W "NORMAL LOW_POWER PPS NMEA" -- "${cur}") )
            return 0
            ;;
        --mode2|--m2)
            COMPREPLY=( $(compgen -W "NORMAL LOW_POWER" -- "${cur}") )
            return 0
            ;;
        --pll)
            COMPREPLY=( $(compgen -W "1 0 true false" -- "${cur}") )
            return 0
            ;;
    esac

    COMPREPLY=( $(compgen -W "${options}" -- "${cur}") )
    return 0
}

complete -F _gpsdo_completions gpsdo
